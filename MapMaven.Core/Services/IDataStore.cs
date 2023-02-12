using Microsoft.EntityFrameworkCore;

namespace MapMaven.Core.Services
{
    public interface IDataStore
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
