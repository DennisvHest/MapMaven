using BeatSaberTools.Infrastructure;
using BeatSaberTools.Worker;
using BeatSaberTools.Worker.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Beat Saber Tools Worker";
    })
    .ConfigureServices(services =>
    {
        services.AddBeatSaberTools<BeatSaberWorkerFileService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
