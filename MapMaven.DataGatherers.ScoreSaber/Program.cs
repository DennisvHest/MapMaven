using MapMaven.DataGatherers.ScoreSaber;
using MapMaven.DataGatherers.ScoreSaber.Data;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();

        services.AddDbContext<ScoreSaberScoresContext>(options =>
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("ScoreSaberScoresConnection")),
            contextLifetime: ServiceLifetime.Singleton
        );

        services.AddHttpClient<ScoreSaberApiClient>(client => client.BaseAddress = new Uri("https://scoresaber.com"));
    })
    .Build();

host.Run();
