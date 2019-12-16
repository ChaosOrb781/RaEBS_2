using GrainInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;

namespace SiloMain
{
    class Program
    {
        static void Main(string[] args)
        {
            RunTestCluster();
        }

        static async void RunTestCluster()
        {
            TestCluster cluster = new ClusterFixture().Cluster;
            int playerCount = 5;
            int ballCount = 3;
            List<Guid> players = new List<Guid>();
            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new Guid());
            }
            for (int i = 0; i < playerCount; i++)
            {
                IPlayer player = cluster.Client.GetGrain<IPlayer>(players[i]);
                await player.Initialize(players, i < ballCount);
            }
            for (int i = 0; i < playerCount; i++)
            {
                IPlayer testPlayer = cluster.Client.GetGrain<IPlayer>(players[0]);
                List<Guid> balls = await testPlayer.GetBallIds();
                System.Diagnostics.Debug.WriteLine("Player {0} {1}", i, balls.Count > 0 ? "is holding a ball" : "is not holding a ball");
            }
            System.Diagnostics.Debug.WriteLine("Test");
        }
    }
}
