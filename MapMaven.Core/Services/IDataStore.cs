using Microsoft.EntityFrameworkCore;

namespace MapMaven.Core.Services
{
    public interface IDataStore : IDisposable
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
