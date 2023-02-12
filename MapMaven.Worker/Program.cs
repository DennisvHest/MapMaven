using MapMaven.Core.Services;
using MapMaven.Infrastructure;
using MapMaven.Worker;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Map Maven Worker";
    })
    .ConfigureServices(services =>
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Join(BeatSaberFileService.AppDataLocation, "logs", "worker-logs", "worker-log-.txt"),
                rollingInterval: RollingInterval.Day
            ).CreateLogger();

        services.AddLogging(loggingBuilder =>
          loggingBuilder.AddSerilog(dispose: true));

        services.AddMapMaven();

        services.AddHostedService<Worker>();
    })
    .Build();

StartupSetup.Initialize(host.Services);

await host.RunAsync();
