using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>EF Core implementation of <see cref="ICountryRepository"/>.</summary>
public class EfCoreCountryRepository : ICountryRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreCountryRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Country> GetOrCreateAsync(string displayName, string? countryCode,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Countries
            .FirstOrDefaultAsync(c => c.DisplayName == displayName, cancellationToken);

        if (existing is not null)
        {
            if (existing.CountryCode is null && countryCode is not null)
                existing.CountryCode = countryCode;
            return existing;
        }

        var created = new Country
        {
            Id = CountryId.New(),
            DisplayName = displayName,
            CountryCode = countryCode
        };
        _context.Countries.Add(created);
        return created;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
