using Microsoft.EntityFrameworkCore;

namespace BeatSaberTools.Core.Services
{
    public interface IDataStore
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
