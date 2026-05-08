using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>EF Core implementation of <see cref="ISectorRepository"/>.</summary>
public class EfCoreSectorRepository : ISectorRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreSectorRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Sector> GetOrCreateAsync(string displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Sectors
            .FirstOrDefaultAsync(s => s.DisplayName == displayName, cancellationToken);

        if (existing is not null)
            return existing;

        var created = new Sector
        {
            Id = SectorId.New(),
            DisplayName = displayName
        };
        _context.Sectors.Add(created);
        return created;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
