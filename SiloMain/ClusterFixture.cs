using Grains;
using Orleans;
using Orleans.ApplicationParts;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using System.Net;

namespace SiloMain
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
               .Configure<ClusterOptions>(options =>
               {
                   options.ClusterId = "dev";
                   options.ServiceId = "OrleansStorage";
               })
               .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
               .AddAdoNetGrainStorageAsDefault(options =>
               {
                   options.Invariant = "Npgsql";
                   options.ConnectionString = "Server=127.0.0.1;Port=5432;Database=OrleansStorage;User Id=postgres;Password=postgres;";
                   options.UseJsonFormat = true;
               })
               .UseInMemoryReminderService();
        }
    }
}
