using GrainInterfaces;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTests
{
    public class IPlayerTests : IClassFixture<ClusterFixture>
    {
        ClusterFixture fixture;
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;

        public IPlayerTests(ClusterFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _cluster = fixture.Cluster;
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ClientIsInitialized()
        {
            Assert.True(_cluster.Client.IsInitialized);
        }

        [Fact]
        public void FetchPlayer()
        {
            IPlayer player = _cluster.Client.GetGrain<IPlayer>(new Guid());
            Assert.NotNull(player);
        }

        [Fact]
        public async void InitializePlayerWithoutBall()
        {
            IPlayer player = _cluster.Client.GetGrain<IPlayer>(new Guid());
            await player.Initialize(new List<Guid>(), false);
            List<Guid> balls = await player.GetBallIds();
            Assert.Empty(balls);
        }

        [Fact]
        public async void InitializePlayerWithBall()
        {
            IPlayer player = _cluster.Client.GetGrain<IPlayer>(new Guid());
            await player.Initialize(new List<Guid>(), true);
            List<Guid> balls = await player.GetBallIds();
            Assert.Single(balls);
        }
    }
}
