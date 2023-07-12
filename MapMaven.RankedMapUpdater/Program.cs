using MapMaven.RankedMapUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MapMaven.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddMapMaven();

        services.AddScoped<RankedMapService>();
    })
    .Build();

host.Run();
