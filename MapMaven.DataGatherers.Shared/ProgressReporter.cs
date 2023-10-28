using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MapMaven.DataGatherers.Shared
{
    public class ProgressReporter : IDisposable
    {
        private Stopwatch _stopwatch;

        private ILogger _logger;

        private int _totalItems;
        private int _itemsPerPage;
        private int _completedPages;

        public ProgressReporter(int totalItems, int itemsPerPage, ILogger logger, int alreadyDone = 0)
        {
            _stopwatch = Stopwatch.StartNew();

            _totalItems = totalItems;
            _itemsPerPage = itemsPerPage;
            _logger = logger;
            _completedPages = alreadyDone;
        }

        public void ReportProgress()
        {
            _completedPages++;

            var totalPages = _totalItems / _itemsPerPage;

            var averageTime = _stopwatch.Elapsed / _completedPages;
            var estimatedTimeLeft = averageTime * (totalPages - _completedPages);

            _logger.LogInformation($"Fetched: {_completedPages}/{totalPages} ({(double)_completedPages / totalPages:#0.##%}). Average request duration: {averageTime}. Elapsed time: {_stopwatch.Elapsed} Estimated time left: {estimatedTimeLeft}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
        }
    }
}
