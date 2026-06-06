using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using VendorRisk.Api.Endpoints.V1;
using VendorRisk.Application;
using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;
using VendorRisk.Infrastructure;
using VendorRisk.Infrastructure.Persistence;
using VendorRisk.Infrastructure.RiskData;

// Bootstrap logger: JSON (compact) to the console — captures very early startup/fatal logs.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        // Structured JSON to a daily rolling file (ELK/Filebeat-ready).
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/log-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true));

    // Caching is enabled only when Redis is configured (both the decorator and the
    // distributed cache are gated on the same connection string).
    bool hasRedis = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Redis"));

    builder.Services.AddApplication(enableCaching: hasRedis);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    WebApplication app = builder.Build();

    app.UseSerilogRequestLogging();

    // Apply migrations and seed before serving (retries while PostgreSQL warms up in Docker).
    await InitializeDatabaseAsync(app);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapVendorEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Vendor Risk API terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    IServiceProvider services = scope.ServiceProvider;

    AppDbContext db = services.GetRequiredService<AppDbContext>();

    const int maxAttempts = 10;
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch when (attempt < maxAttempts)
        {
            Log.Warning("Database not ready (attempt {Attempt}/{Max}); retrying…", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    RiskDataOptions options = services.GetRequiredService<RiskDataOptions>();

    // Seed vendors from SampleVendorData.json — only when the table is empty.
    IVendorRepository vendors = services.GetRequiredService<IVendorRepository>();
    if (!await vendors.AnyAsync())
    {
        IReadOnlyList<VendorProfile> seed = SampleVendorLoader.Load(options.SeedPath);
        if (seed.Count > 0)
        {
            await vendors.AddRangeAsync(seed);
            Log.Information("Seeded {Count} vendors from {Path}.", seed.Count, options.SeedPath);
        }
    }

    // Seed the risk matrix (3 tables) from RiskFactorMatrix.json — only when empty.
    IRiskMatrixRepository matrix = services.GetRequiredService<IRiskMatrixRepository>();
    if (!await matrix.AnyAsync())
    {
        RiskMatrixGraph graph = RiskMatrixSeedLoader.Load(options.MatrixPath);
        if (graph.Edges.Count > 0)
        {
            await matrix.SeedAsync(graph.Categories, graph.Nodes, graph.Edges);
            Log.Information(
                "Seeded risk matrix from {Path}: {Categories} categories, {Nodes} nodes, {Edges} edges.",
                options.MatrixPath, graph.Categories.Count, graph.Nodes.Count, graph.Edges.Count);
        }
    }

    // Warm the in-memory matrix cache from the database (now the source of truth).
    await services.GetRequiredService<IRiskMatrixProvider>().ReloadAsync();
}
