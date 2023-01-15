using BeatSaberTools.Infrastructure;
using BeatSaberTools.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Beat Saber Tools Worker";
    })
    .ConfigureServices(services =>
    {
        services.AddBeatSaberTools();

        services.AddHostedService<Worker>();
    })
    .Build();

StartupSetup.Initialize(host.Services);

await host.RunAsync();
