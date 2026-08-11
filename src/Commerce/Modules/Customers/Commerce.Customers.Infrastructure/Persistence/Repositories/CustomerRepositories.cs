using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Customers.Infrastructure.Persistence.Repositories;

internal sealed class EfCustomerRepository(CommerceDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Customer>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Customer?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Customer>()
            .FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId && !x.Deleted, cancellationToken);

    public Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Set<Customer>()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && !x.Deleted, cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Customer>().AsQueryable();
        if (!includeDeleted)
        {
            query = query.Where(x => !x.Deleted);
        }

        return await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Customer>().Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Customer>().Update(customer);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EfCustomerAddressRepository(CommerceDbContext dbContext) : ICustomerAddressRepository
{
    public Task<CustomerAddress?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Set<CustomerAddress>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerAddress>> ListByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Set<CustomerAddress>()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerAddress>().Add(address);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerAddress address, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerAddress>().Update(address);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(CustomerAddress address, CancellationToken cancellationToken = default)
    {
        dbContext.Set<CustomerAddress>().Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
