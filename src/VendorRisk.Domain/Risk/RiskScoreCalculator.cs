using VendorRisk.Domain.Entities;
using VendorRisk.Domain.Enums;

namespace VendorRisk.Domain.Risk;

/// <summary>
/// Rule-based, deterministic and explainable scoring engine. Base rules map raw vendor fields to
/// triggered factors; similarity propagation then amplifies each factor by its correlated risks
/// (severity = base + (1 - base) · β · avg(neighbour weights), kept in [0,1]). Factors in a
/// dimension are combined with a noisy-OR, and the final score is the weighted sum of the three
/// dimensions. The method is pure, so the same inputs always produce the same output.
/// </summary>
public static class RiskScoreCalculator
{
    public static RiskAssessment Calculate(
        VendorProfile vendor,
        RiskMatrix matrix,
        RiskScoringOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(matrix);
        options ??= RiskScoringOptions.Default;

        DimensionScore financial = ScoreDimension(
            RiskDimension.Financial, BuildFinancialFactors(vendor, matrix, options));
        DimensionScore operational = ScoreDimension(
            RiskDimension.Operational, BuildOperationalFactors(vendor, matrix, options));
        DimensionScore security = ScoreDimension(
            RiskDimension.Security, BuildSecurityFactors(vendor, matrix, options));

        double finalScore =
            financial.Score * options.FinancialWeight +
            operational.Score * options.OperationalWeight +
            security.Score * options.SecurityWeight;

        finalScore = Math.Round(Clamp01(finalScore), 2, MidpointRounding.AwayFromZero);

        IReadOnlyList<RiskFactor> allFactors =
        [
            .. financial.Factors,
            .. operational.Factors,
            .. security.Factors,
        ];
        IReadOnlyList<RiskFactor> ranked = [.. allFactors.OrderByDescending(f => f.Severity)];

        RiskLevel level = DetermineLevel(finalScore, options);
        string reason = BuildReason(ranked, level, options);

        return new RiskAssessment(
            vendor.Id,
            finalScore,
            level,
            reason,
            [financial, operational, security],
            ranked);
    }

    // ----- Dimension aggregation -------------------------------------------------------

    private static DimensionScore ScoreDimension(RiskDimension dimension, List<RiskFactor> factors)
        => new(dimension, NoisyOr(factors), factors);

    /// <summary>Probabilistic OR: 1 − Π(1 − sᵢ). Bounded in [0,1]; 0 when there are no factors.</summary>
    private static double NoisyOr(IReadOnlyList<RiskFactor> factors)
    {
        double complement = 1d;
        foreach (RiskFactor factor in factors)
        {
            complement *= 1d - factor.Severity;
        }

        return Clamp01(1d - complement);
    }

    // ----- Factor builders (the base rules) --------------------------------------------

    private static List<RiskFactor> BuildFinancialFactors(
        VendorProfile vendor, RiskMatrix matrix, RiskScoringOptions options)
    {
        int fh = vendor.FinancialHealth;

        // A baseline factor is always emitted so the financial dimension is never silently zero.
        // Only the distress band ("lowCashFlow") exists in the matrix and gets propagated.
        (string code, string label, double severity, string description) = fh switch
        {
            < 50 => ("lowCashFlow", "Low financial health",
                0.85, $"Financial health is critically low ({fh}/100): elevated cash-flow and solvency risk."),
            < 65 => ("weakRevenueStability", "Weak financial standing",
                0.50, $"Financial health is below average ({fh}/100): moderate financial risk."),
            <= 80 => ("financialWatch", "Moderate financial standing",
                0.30, $"Financial health is acceptable but not strong ({fh}/100): mild financial risk."),
            _ => ("financialStable", "Strong financial health",
                0.10, $"Financial health is strong ({fh}/100): low financial risk."),
        };

        return [CreateFactor(code, RiskDimension.Financial, label, description, severity, matrix, options)];
    }

    private static List<RiskFactor> BuildOperationalFactors(
        VendorProfile vendor, RiskMatrix matrix, RiskScoringOptions options)
    {
        List<RiskFactor> factors = [];

        if (vendor.SlaUptime < options.SlaThreshold)
        {
            // Deeper SLA shortfalls are more severe, capped so a single factor can't dominate.
            double severity = Math.Min(0.30 + (options.SlaThreshold - vendor.SlaUptime) * 0.05, 0.90);
            factors.Add(CreateFactor(
                "slaDrop", RiskDimension.Operational, $"SLA below {options.SlaThreshold}%",
                $"SLA uptime {vendor.SlaUptime}% is below the {options.SlaThreshold}% target.",
                severity, matrix, options));
        }

        if (vendor.MajorIncidents > 0)
        {
            double severity = vendor.MajorIncidents switch
            {
                >= 4 => 0.90,
                3 => 0.80,
                2 => 0.55,
                _ => 0.30,
            };
            string label = vendor.MajorIncidents >= 3 ? "Recurring major incidents" : "Major incidents";
            factors.Add(CreateFactor(
                "majorIncident", RiskDimension.Operational, label,
                $"{vendor.MajorIncidents} major incident(s) reported in the last 12 months.",
                severity, matrix, options));
        }

        return factors;
    }

    private static List<RiskFactor> BuildSecurityFactors(
        VendorProfile vendor, RiskMatrix matrix, RiskScoringOptions options)
    {
        List<RiskFactor> factors = [];

        if (!HasIso27001(vendor.SecurityCerts))
        {
            factors.Add(CreateFactor(
                "missingISO27001", RiskDimension.Security, "Missing ISO27001",
                "ISO27001 certification is not held: elevated security risk.",
                0.60, matrix, options));
        }

        // Tri-state documents: only an explicit `false` is a failure. `null` = not assessed.
        if (vendor.Documents.PentestReportValid == false)
        {
            factors.Add(CreateFactor(
                "failedPenTest", RiskDimension.Security, "Failed penetration test",
                "Penetration test report is invalid or failed: critical security risk.",
                1.00, matrix, options));
        }

        if (vendor.Documents.PrivacyPolicyValid == false)
        {
            factors.Add(CreateFactor(
                "expiredPrivacyPolicy", RiskDimension.Security, "Privacy policy expired",
                "Privacy policy is expired or invalid: moderate compliance risk.",
                0.40, matrix, options));
        }

        if (vendor.Documents.ContractValid == false)
        {
            factors.Add(CreateFactor(
                "expiredContract", RiskDimension.Security, "Contract invalid",
                "Contract is expired or invalid: compliance risk.",
                0.50, matrix, options));
        }

        return factors;
    }

    // ----- Similarity propagation ------------------------------------------------------

    /// <summary>
    /// Builds a factor and amplifies its base severity using every edge reachable from it in the
    /// matrix: severity = base + (1 - base) · β · avg(collected weights). For display, the
    /// collected risks are de-duplicated (max weight) and sorted.
    /// </summary>
    private static RiskFactor CreateFactor(
        string code,
        RiskDimension dimension,
        string label,
        string description,
        double baseSeverity,
        RiskMatrix matrix,
        RiskScoringOptions options)
    {
        baseSeverity = Clamp01(baseSeverity);

        IReadOnlyList<CorrelatedRisk> effects = matrix.CollectEffects(code);
        double average = effects.Count == 0 ? 0d : effects.Average(e => e.Weight);

        double severity = baseSeverity
            + (1d - baseSeverity) * options.PropagationDamping * average;

        // For display only: drop the factor's own code and keep the strongest weight per risk.
        // The average above still counts every edge.
        IReadOnlyList<CorrelatedRisk> correlated =
        [
            .. effects
                .Where(e => !string.Equals(e.Name, code, StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CorrelatedRisk(g.Key, g.Max(x => x.Weight)))
                .OrderByDescending(c => c.Weight),
        ];

        return new RiskFactor(
            code,
            dimension,
            label,
            description,
            Math.Round(baseSeverity, 4),
            Math.Round(Clamp01(severity), 4),
            correlated);
    }

    // ----- Level + reason --------------------------------------------------------------

    // The level follows the weighted score directly, so level and score never disagree.
    private static RiskLevel DetermineLevel(double score, RiskScoringOptions options) =>
        score >= options.CriticalThreshold ? RiskLevel.Critical :
        score >= options.HighThreshold ? RiskLevel.High :
        score >= options.MediumThreshold ? RiskLevel.Medium :
        RiskLevel.Low;

    private static string BuildReason(
        IReadOnlyList<RiskFactor> factors, RiskLevel level, RiskScoringOptions options)
    {
        List<RiskFactor> concerns =
            [.. factors.Where(f => f.BaseSeverity >= options.ReasonThreshold)];

        if (concerns.Count == 0)
        {
            return "No significant risk factors identified; the vendor meets financial, " +
                   "operational, and security expectations.";
        }

        // Names only the vendor's own triggered factors, not the matrix's correlated risks.
        string headline = string.Join(" + ", concerns.Select(c => c.Label));
        return $"Overall {level} risk, driven by: {headline}.";
    }

    // ----- Helpers ---------------------------------------------------------------------

    private static bool HasIso27001(IEnumerable<string> certs) =>
        certs.Any(c => Normalize(c) == "iso27001");

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
}
