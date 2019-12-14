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
    [Collection(ClusterCollection.Name)]
    public class IPlayerTests
    {
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;

        public IPlayerTests(ClusterFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
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
        public async void PlayerGetsBall()
        {
            IPlayer player = _cluster.GrainFactory.GetGrain<IPlayer>(new Guid());
            Guid newBall = new Guid();
            await player.ReceiveBall(newBall);
            List<Guid> balls = await player.GetBallIds();
            Assert.Contains(newBall, balls);
        }
    }
}
