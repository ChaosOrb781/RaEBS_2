using Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using Xunit;
using Microsoft.Extensions.Logging;

namespace XUnitTests
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<MySiloBuilderConfigurator>();
            this.Cluster = builder.Build();
            this.Cluster.Deploy();
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }

    public class MySiloBuilderConfigurator : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder
                .UseLocalhostClustering()
                .ConfigureDefaults()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                .AddAdoNetGrainStorageAsDefault(options =>
                {
                    options.Invariant = Statics.Values.SQLInvariant;
                    options.ConnectionString = Statics.Values.ConnectionString;
                    options.UseJsonFormat = true;
                })
               .UseInMemoryReminderService();
        }
    }

    [CollectionDefinition(Statics.Values.ClusterCollection)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture> { }
}
