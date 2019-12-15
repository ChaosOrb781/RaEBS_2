using GrainInterfaces;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;

namespace SiloMain
{
    class Program
    {
        static void Main(string[] args)
        {
            var fixture = new ClusterFixture();
            TestCluster _cluster = fixture.Cluster;

            IPlayer player = _cluster.Client.GetGrain<IPlayer>(new Guid());
            player.Initialize(new List<Guid>(), true);

        }
    }
}
