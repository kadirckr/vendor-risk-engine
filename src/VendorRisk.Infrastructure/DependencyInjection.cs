using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VendorRisk.Application.Abstractions;
using VendorRisk.Infrastructure.Ai;
using VendorRisk.Infrastructure.Persistence;
using VendorRisk.Infrastructure.RiskData;

namespace VendorRisk.Infrastructure;

/// <summary>
/// DI registrations for the Infrastructure layer: the EF Core context (PostgreSQL),
/// the repositories, and the database-backed risk matrix provider.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("VendorRiskDb")
            ?? "Host=localhost;Port=5432;Database=vendorrisk;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IRiskMatrixRepository, RiskMatrixRepository>();

        // Seed-file locations (defaults point at the copied Data folder; overridable via config).
        RiskDataOptions riskData = new();
        configuration.GetSection(RiskDataOptions.SectionName).Bind(riskData);
        services.AddSingleton(riskData);

        // The matrix is read from the DB and cached in memory → singleton.
        services.AddSingleton<IRiskMatrixProvider, DbRiskMatrixProvider>();

        // Distributed cache: ONLY when Redis is configured. No Redis → no cache at all
        // (the caching decorator is likewise registered only when caching is enabled).
        string? redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }

        // AI-assisted, human-readable risk reason. Enabled only when an OpenAI key is configured;
        // with no key (or on any AI failure) the deterministic rule-based reason is used unchanged.
        // The humanizer talks to an IChatClient, so the provider can be swapped here alone.
        string? openAiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            string model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            services.AddChatClient(_ =>
                new OpenAI.Chat.ChatClient(model, openAiKey).AsIChatClient());
            services.AddScoped<IReasonHumanizer, ChatClientReasonHumanizer>();
        }
        else
        {
            services.AddScoped<IReasonHumanizer, PassthroughReasonHumanizer>();
        }

        return services;
    }
}
