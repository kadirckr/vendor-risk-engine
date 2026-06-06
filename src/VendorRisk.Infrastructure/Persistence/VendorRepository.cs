using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendorRisk.Application.Abstractions;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Infrastructure.Persistence;

/// <summary>PostgreSQL/EF Core implementation of <see cref="IVendorRepository"/>.</summary>
internal sealed class VendorRepository(AppDbContext db, ILogger<VendorRepository> logger) : IVendorRepository
{
    /// <inheritdoc />
    public async Task<ErrorOr<VendorProfile>> AddAsync(VendorProfile vendor, CancellationToken ct = default)
    {
        try
        {
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync(ct);
            return vendor;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save vendor {VendorName}.", vendor.Name);
            return Error.Failure("Vendor.AddFailed", $"Failed to save the vendor: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<VendorProfile?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VendorProfile>> GetAllAsync(CancellationToken ct = default) =>
        await db.Vendors.AsNoTracking().OrderBy(v => v.Id).ToListAsync(ct);

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken ct = default) => db.Vendors.AnyAsync(ct);

    /// <inheritdoc />
    public async Task<ErrorOr<Success>> AddRangeAsync(
        IEnumerable<VendorProfile> vendors, CancellationToken ct = default)
    {
        try
        {
            db.Vendors.AddRange(vendors);
            await db.SaveChangesAsync(ct);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed vendors.");
            return Error.Failure("Vendor.SeedFailed", $"Failed to seed vendors: {ex.Message}");
        }
    }
}
