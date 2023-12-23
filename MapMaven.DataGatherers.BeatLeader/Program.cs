using MapMaven.DataGatherers.BeatLeader;
using MapMaven.DataGatherers.BeatLeader.Data;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();

        services.AddDbContext<BeatLeaderScoresContext>(options =>
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("BeatLeaderScoresConnection")),
            contextLifetime: ServiceLifetime.Singleton
        );

        services.AddHttpClient<BeatLeaderApiClient>(client => client.BaseAddress = new Uri("https://api.beatleader.xyz"));
    })
    .Build();

host.Run();
