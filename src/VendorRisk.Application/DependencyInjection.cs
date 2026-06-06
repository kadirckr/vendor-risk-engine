using Microsoft.Extensions.DependencyInjection;
using VendorRisk.Application.Abstractions;
using VendorRisk.Application.BuildingBlocks;
using VendorRisk.Application.Services;
using VendorRisk.Application.UseCases.CreateVendor;
using VendorRisk.Application.UseCases.GetVendorRisk;
using VendorRisk.Application.UseCases.ListVendors;
using VendorRisk.Application.Vendors;

namespace VendorRisk.Application;

/// <summary>
/// DI registrations for the Application layer: the dispatcher, the rule engine, and the
/// use-case handlers.
/// </summary>
public static class DependencyInjection
{
    /// <param name="enableCaching">
    /// When true, the rule engine is wrapped in <see cref="CachingRuleEngineService"/>
    /// (requires an <c>IDistributedCache</c>, e.g. Redis). When false, the plain engine is used.
    /// </param>
    public static IServiceCollection AddApplication(this IServiceCollection services, bool enableCaching = false)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Decorator chain: Caching → Humanizing → RuleEngine. The humanizing layer (AI reason)
        // always runs; caching, when enabled, wraps it so the humanized reason is cached and the
        // AI backend is not re-hit for identical vendor inputs.
        services.AddScoped<RuleEngineService>();
        services.AddScoped<HumanizingRuleEngineService>();
        if (enableCaching)
        {
            services.AddScoped<IRuleEngineService, CachingRuleEngineService>();
        }
        else
        {
            services.AddScoped<IRuleEngineService>(sp => sp.GetRequiredService<HumanizingRuleEngineService>());
        }

        // Use-case handlers
        services.AddScoped<IRequestHandler<CreateVendorRequest, CreateVendorResult>, CreateVendorHandler>();
        services.AddScoped<IRequestHandler<GetVendorRiskRequest, RiskAssessmentResponse>, GetVendorRiskHandler>();
        services.AddScoped<IRequestHandler<ListVendorsRequest, ListVendorsResult>, ListVendorsHandler>();

        return services;
    }
}
