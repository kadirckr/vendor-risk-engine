using Microsoft.EntityFrameworkCore;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Infrastructure.Persistence;

/// <summary>EF Core context for the vendors and the normalized risk-matrix tables.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VendorProfile> Vendors => Set<VendorProfile>();

    public DbSet<RiskCategory> RiskCategories => Set<RiskCategory>();

    public DbSet<RiskFactorNode> RiskFactorNodes => Set<RiskFactorNode>();

    public DbSet<RiskFactorEdge> RiskFactorEdges => Set<RiskFactorEdge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table 1: categories
        modelBuilder.Entity<RiskCategory>(entity =>
        {
            entity.ToTable("risk_categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).IsRequired();
            entity.HasIndex(c => c.Code).IsUnique();
        });

        // Table 2: factor nodes (each belongs to a category)
        modelBuilder.Entity<RiskFactorNode>(entity =>
        {
            entity.ToTable("risk_factors");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Code).IsRequired();
            entity.HasIndex(f => f.Code).IsUnique();

            entity.HasOne(f => f.Category)
                .WithMany()
                .HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Table 3: weighted parent → child edges
        modelBuilder.Entity<RiskFactorEdge>(entity =>
        {
            entity.ToTable("risk_factor_edges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Weight).IsRequired();

            // The same parent → child pair cannot appear twice.
            entity.HasIndex(e => new { e.ParentFactorId, e.ChildFactorId }).IsUnique();

            // Restrict (not cascade) — two FKs into the same table would create multiple
            // cascade paths otherwise.
            entity.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.ParentFactorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Child)
                .WithMany()
                .HasForeignKey(e => e.ChildFactorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorProfile>(entity =>
        {
            entity.ToTable("vendors");
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Name).IsRequired();
            entity.Property(v => v.FinancialHealth).IsRequired();
            entity.Property(v => v.SlaUptime).IsRequired();
            entity.Property(v => v.MajorIncidents).IsRequired();

            // Certifications are stored as a PostgreSQL text[] column.
            entity.Property(v => v.SecurityCerts).HasColumnType("text[]");

            // Document validity flags are an owned value object → columns on the same table.
            entity.OwnsOne(v => v.Documents, documents =>
            {
                documents.Property(d => d.ContractValid).HasColumnName("contract_valid");
                documents.Property(d => d.PrivacyPolicyValid).HasColumnName("privacy_policy_valid");
                documents.Property(d => d.PentestReportValid).HasColumnName("pentest_report_valid");
            });
        });
    }
}
