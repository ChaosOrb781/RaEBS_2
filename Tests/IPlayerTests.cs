using GrainInterfaces;
using Grains;
using Orleans;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTests
{
    public class IPlayerTests
    {
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;

        public IPlayerTests(ClusterFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _cluster = fixture.Cluster;
        }

        //TODO: Implements tests
        [Fact]
        public Task Test0()
        {
            Assert.True(true);
            return Task.Factory.StartNew(() => Assert.True(true));
        }
    }
}
