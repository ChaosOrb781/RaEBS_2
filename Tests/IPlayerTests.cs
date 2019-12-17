using GrainInterfaces;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTests
{
    public class IPlayerTests : IClassFixture<ClusterFixture>
    {
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;

        //Statics.Players contains 10 guids we reuse for testing purposes
        public IPlayerTests(ClusterFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _cluster = fixture.Cluster;
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ClusterHasActivePrimarySilo()
        {
            Assert.True(_cluster.Silos.Count > 0);
            Assert.NotNull(_cluster.Primary);
        }

        [Fact]
        public void ClientIsInitialized()
        {
            Assert.True(_cluster.Client.IsInitialized);
        }

        [Fact]
        public void FetchPlayer()
        {
            IPlayer player = _cluster.Client.GetGrain<IPlayer>(Statics.Values.Players[0]);
            Assert.NotNull(player);
        }

        [Fact]
        public async void InitializePlayersWithoutBalls()
        {
            try
            {
                List<Task> initializers = new List<Task>();
                List<IPlayer> players = new List<IPlayer>();
                foreach (Guid playerId in Statics.Values.Players)
                {
                    IPlayer player = _cluster.Client.GetGrain<IPlayer>(playerId);
                    players.Add(player);
                    initializers.Add(player.Initialize(Statics.Values.Players, false));
                }
                await Task.WhenAll(initializers);
                List<Task<List<Guid>>> balls = new List<Task<List<Guid>>>();
                foreach (IPlayer player in players)
                {
                    balls.Add(player.GetBallIds());
                }
                await Task.WhenAll(balls);

                Assert.True(balls.TrueForAll(l => l.Result.Count == 0));
            } 
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Error: " + e.Message);
                Assert.False(true);
            }
        }

        [Fact]
        public async void InitializePlayersWithBalls()
        {
            try
            {
                List<Task> initializers = new List<Task>();
                List<IPlayer> players = new List<IPlayer>();
                foreach (Guid playerId in Statics.Values.Players)
                {
                    IPlayer player = _cluster.Client.GetGrain<IPlayer>(playerId);
                    players.Add(player);
                    initializers.Add(player.Initialize(Statics.Values.Players, true));
                }
                await Task.WhenAll(initializers);

                List<Task<List<Guid>>> balls = new List<Task<List<Guid>>>(); 
                foreach (IPlayer player in players)
                {
                    balls.Add(player.GetBallIds());
                }
                await Task.WhenAll(balls);

                Assert.True(balls.TrueForAll(l => l.Result.Count == 1));
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Error: " + e.Message);
                Assert.False(true);
            }
        }

        [Fact]
        public async void InitializeHalfOfPlayersWithBalls()
        {
            try
            {
                List<Task> initializers = new List<Task>();
                List<IPlayer> players = new List<IPlayer>();
                for (int i = 0; i < Statics.Values.Players.Length; i++)
                {
                    IPlayer player = _cluster.Client.GetGrain<IPlayer>(Statics.Values.Players[i]);
                    players.Add(player);
                    initializers.Add(player.Initialize(Statics.Values.Players, i < (Statics.Values.Players.Length + 1) / 2));
                }
                await Task.WhenAll(initializers);
                List<Task<List<Guid>>> balls = new List<Task<List<Guid>>>();
                foreach (IPlayer player in players)
                {
                    balls.Add(player.GetBallIds());
                }
                await Task.WhenAll(balls);

                Assert.Equal(5, balls.FindAll(ball => ball.Result.Count == 1).Count);
                Assert.Equal(5, balls.FindAll(ball => ball.Result.Count == 0).Count);
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Error: " + e.Message);
                Assert.False(true);
            }
        }

        [Fact]
        public async void TorseAllKBallsAtOnePlayer()
        {
            try
            {
                List<Task> initializers = new List<Task>();
                List<IPlayer> players = new List<IPlayer>();
                foreach (Guid playerId in Statics.Values.Players)
                {
                    IPlayer player = _cluster.Client.GetGrain<IPlayer>(playerId);
                    players.Add(player);
                    initializers.Add(player.Initialize(Statics.Values.Players, false));
                }
                await Task.WhenAll(initializers);

                List<Task> torses = new List<Task>();
                for (int i = 0; i < Statics.Values.Kmax; i++)
                {
                    torses.Add(players[0].ReceiveBall(Statics.Values.Balls[i]));
                }
                await Task.WhenAll(torses);

                List<Task<List<Guid>>> balls = new List<Task<List<Guid>>>();
                foreach (IPlayer player in players)
                {
                    balls.Add(player.GetBallIds());
                }
                await Task.WhenAll(balls);

                _testOutputHelper.WriteLine("Right after statistics:");
                //Check immidially after
                int numBalls = 0;
                for (int i = 0; i < Statics.Values.N; i++)
                {
                    numBalls += balls[i].Result.Count;
                    _testOutputHelper.WriteLine("Player {0} had {1} balls", i, balls[i].Result.Count);
                }
                Assert.Equal(Statics.Values.Kmax, numBalls);
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Error: " + e.Message);
                Assert.False(true);
            }
        }
    }
}
