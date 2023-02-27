using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MapMaven.Utility
{
    /// <summary>
    /// Using copied HostServiceExecutor from ASP.NET Core because .NET MAUI is not yet able to execute BackgroundServices.
    /// Source: https://github.com/dotnet/aspnetcore/blob/3ea008c80d5cc63de7f90ddfd6823b7b006251ff/src/Hosting/Hosting/src/Internal/HostedServiceExecutor.cs
    /// </summary>
    public class HostedServiceExecutor
    {
        private readonly IEnumerable<IHostedService> _services;
        private readonly ILogger<HostedServiceExecutor> _logger;

        public HostedServiceExecutor(ILogger<HostedServiceExecutor> logger, IEnumerable<IHostedService> services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StartAsync(token));
        }

        public Task StopAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StopAsync(token), throwOnFirstFailure: false);
        }

        private async Task ExecuteAsync(Func<IHostedService, Task> callback, bool throwOnFirstFailure = true)
        {
            List<Exception>? exceptions = null;

            foreach (var service in _services)
            {
                try
                {
                    await callback(service);
                }
                catch (Exception ex)
                {
                    if (throwOnFirstFailure)
                    {
                        throw;
                    }

                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            // Throw an aggregate exception if there were any exceptions
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
