namespace MapMaven.RankedMapUpdater.Services
{
    public interface IRankedMapService
    {
        Task UpdateRankedMapsAsync(DateTime lastRunDate, CancellationToken cancellationToken = default);
    }
}
