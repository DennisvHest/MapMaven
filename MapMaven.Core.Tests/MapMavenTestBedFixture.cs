using MapMaven.Infrastructure;
using MapMaven.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MapMaven.Core.Tests
{
    public class MapMavenTestBedFixture : TestBedFixture
    {
        private SqliteConnection _dbConnection;

        protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        {
            services.AddMapMaven(useStatefulServices: true);

            _dbConnection = new SqliteConnection("DataSource=:memory:");
            _dbConnection.Open();

            services.RemoveAll<DbContextOptions<MapMavenContext>>();
            services.AddDbContext<MapMavenContext>(options =>
                options.UseSqlite(_dbConnection)
            );
        }

        protected override async ValueTask DisposeAsyncCore() => await _dbConnection.DisposeAsync();

        protected override IEnumerable<TestAppSettings> GetTestAppSettings() => Enumerable.Empty<TestAppSettings>();
    }
}
