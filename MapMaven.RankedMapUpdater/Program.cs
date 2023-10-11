using MapMaven.RankedMapUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MapMaven.Infrastructure;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.Storage.Blobs;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((builder, services) =>
    {
        services.AddAzureClients(clientBuilder =>
        {
            var blobConnection = builder.Configuration["MapMavenStorageConnection"];

            if (builder.HostingEnvironment.IsDevelopment())
            {
                clientBuilder.AddBlobServiceClient(blobConnection);
            }
            else
            {
                clientBuilder.AddBlobServiceClient(new Uri(blobConnection));
            }
            clientBuilder.UseCredential(new DefaultAzureCredential());
        });

        services.AddScoped(serviceProvider =>
        {
            var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();

            return blobServiceClient.GetBlobContainerClient(builder.Configuration["MapMavenStorageContainerName"]);
        });

        services.AddMapMaven();

        services.AddScoped<IRankedMapService, ScoreSaberRankedMapService>();
        services.AddScoped<IRankedMapService, BeatLeaderRankedMapService>();
    })
    .Build();

var mapMavenContainerClient = host.Services.GetRequiredService<BlobContainerClient>();
await mapMavenContainerClient.CreateIfNotExistsAsync();

host.Run();
