using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using Xunit;

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

    class MySiloBuilderConfigurator : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder = new SiloHostBuilder()
               .UseLocalhostClustering()
               .Configure<ClusterOptions>(options =>
               {
                   options.ClusterId = "dev";
                   options.ServiceId = "OrleansBasics";
               })
               // TODO replace with your connection string
               .AddAdoNetGrainStorage("OrleansStorage", options =>
               {
                   options.Invariant = "Npgsql";
                   options.ConnectionString = "Server=127.0.0.1;Port=5432;Database=OrleansStorage;User Id=postgres;Password=postgres;";
                   options.UseJsonFormat = true;
               })
               .UseInMemoryReminderService();
        }
    }

    [CollectionDefinition(ClusterCollection.Name)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture>
    {
        public const string Name = "ClusterCollection";
    }

}
