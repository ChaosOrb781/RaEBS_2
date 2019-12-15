using GrainInterfaces;
using Grains;
using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTests
{
    public class IPlayerTests
    {
        private ClusterFixture fixture;
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;

        public IPlayerTests(/*ClusterFixture fixture, */ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            fixture = new ClusterFixture();
            _cluster = fixture.Cluster;
        }

        [Fact]
        public void PrintTest()
        {
            _testOutputHelper.WriteLine("Hello");
            Assert.True(true);
        }

        [Fact]
        public void PlayerLoaded()
        {
            IPlayer player = _cluster.GrainFactory.GetGrain<IPlayer>(new Guid());
            Assert.NotNull(player);
        }

        [Fact]
        public async void PlayerInitialization()
        {
            List<Guid> players = new List<Guid>() { new Guid(), new Guid() };
            IPlayer player1 = _cluster.GrainFactory.GetGrain<IPlayer>(players[0]);
            IPlayer player2 = _cluster.GrainFactory.GetGrain<IPlayer>(players[1]);
            await player1.Initialize(players, true);
            await player2.Initialize(players, false);
            Assert.True(true);
        }

        [Fact]
        public async void PlayerRecieveBall()
        {
            IPlayer player = _cluster.GrainFactory.GetGrain<IPlayer>(new Guid());
            Guid newBall = new Guid();
            await player.ReceiveBall(newBall);
            List<Guid> balls = await player.GetBallIds();
            Assert.Contains(newBall, balls);
        }
    }
}
