using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISectorRepository"/>.
/// </summary>
public class EfCoreSectorRepository : ISectorRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreSectorRepository"/> class.
    /// </summary>
    public EfCoreSectorRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Sector> GetOrCreateAsync(string displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Sectors
            .FirstOrDefaultAsync(s => s.DisplayName == displayName, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing;

        var created = new Sector
        {
            Id = SectorId.New(),
            DisplayName = displayName
        };

        await _context.Sectors.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return created;
    }

    /// <inheritdoc />
    public async Task<Sector?> FindByDisplayNameAsync(string displayName,
        CancellationToken cancellationToken = default)
    {
        return await _context.Sectors
            .FirstOrDefaultAsync(s => s.DisplayName == displayName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
