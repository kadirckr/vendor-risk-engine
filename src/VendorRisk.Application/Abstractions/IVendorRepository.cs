using ErrorOr;
using VendorRisk.Domain.Entities;

namespace VendorRisk.Application.Abstractions;

/// <summary>Persistence operations for vendors. Implemented in the Infrastructure layer.</summary>
public interface IVendorRepository
{
    /// <summary>Inserts a vendor and returns it with its generated id.</summary>
    Task<ErrorOr<VendorProfile>> AddAsync(VendorProfile vendor, CancellationToken ct = default);

    /// <summary>Returns the vendor with the given id, or null when not found.</summary>
    Task<VendorProfile?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Returns all vendors.</summary>
    Task<IReadOnlyList<VendorProfile>> GetAllAsync(CancellationToken ct = default);

    /// <summary>True when at least one vendor exists (used to guard seeding).</summary>
    Task<bool> AnyAsync(CancellationToken ct = default);

    /// <summary>Bulk-inserts vendors (used by the seeder).</summary>
    Task<ErrorOr<Success>> AddRangeAsync(
        IEnumerable<VendorProfile> vendors, CancellationToken ct = default);
}
