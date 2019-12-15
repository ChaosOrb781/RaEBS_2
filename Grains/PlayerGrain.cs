using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grains
{
    public class PlayerState
    {
        public List<Guid> PlayerIds { set; get; }
        public List<Guid> BallIds { set; get; }
        public Guid LatestBallReceived { set; get; }
    }


    [StorageProvider]
    public class PlayerGrain : Grain<PlayerState>, IPlayer
    {
        private static Tuple<int, int> waitPeriod = Tuple.Create(500, 4000);

        private readonly ILogger logger;

        public PlayerGrain(ILogger<PlayerGrain> logger)
        {
            this.logger = logger;
        }

        //http://dotnet.github.io/orleans/Documentation/grains/grain_identity.html
        public override Task OnActivateAsync()
        {
            logger.LogInformation("Activating grain {0}", this.GetPrimaryKey());
            //Do some fetching from the database to initialize state from persistant memory?
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync() 
        {
            logger.LogInformation("Deactivating grain {0}", this.GetPrimaryKey());
            base.OnDeactivateAsync();
            throw new NotImplementedException();
            //remember state when a player is deactivated
        }

        Task IPlayer.Initialize(List<Guid> playerIds, bool isHoldingBall)
        {
            logger.LogInformation("Initializing players of the game...");
            State.PlayerIds = playerIds;
            if (isHoldingBall)
            {
                State.BallIds = new List<Guid>();
                State.BallIds.Add(new Guid());
            }
            /*
            foreach (Guid id in playerIds) {
                logger.LogInformation("Checking if player {0} is holding a ball", id);
                IPlayer player = GrainFactory.GetGrain<IPlayer>(id);
                //if {} Implemet whether or not the Actor has a state - if not, initialize otherwise go to isHoldingBall
                //playerIds.Add(id);
                if (isHoldingBall)
                {
                    logger.LogInformation("Player {0} is holding a ball", id);
                    //player.ReceiveBall();
                    State.BallIds = new List<Guid>();
                    State.BallIds.Add(new Guid());
                    // HoldOrPassBall(null); Maybe do not hold or pass ball here
                }
                else
                {
                    logger.LogInformation("Player {0} is NOT holding a ball", id);
                    State.BallIds = new List<Guid>();
                    State.BallIds.Add(new Guid());
                    // player.ReceiveBall(new Guid()); Maybe the balls shouldn't be distributed here
                }
            }*/
            return Task.CompletedTask;
        }

        async Task IPlayer.ReceiveBall(Guid ballId)
        {
            logger.LogInformation("Player {0} is receiving ball {1}", this.GetPrimaryKey(), ballId);
            State.BallIds.Add(ballId);
            await WriteStateAsync();
            logger.LogInformation("Player {0} now has {1} balls", this.GetPrimaryKey(), State.BallIds.Count);
            if (State.BallIds.Count > 1)
            {
                PassOtherBalls(ballId);
            }
            else
            {
                HoldOrPassBall(ballId);
            }
        }

        Task<List<Guid>> IPlayer.GetBallIds()
        {
            logger.LogInformation("Someone wants to know about my balls :3");
            return Task.FromResult(State.BallIds);
        }

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            //http://dotnet.github.io/orleans/Documentation/grains/timers_and_reminders.html
            return Task.CompletedTask;
        }

        /// <summary>
        ///The player waits for a random amount of time. When the time is up, 
        ///  the player then decides at random to either keep the ball for another
        ///  period or to pass the ball.
        /// </summary>
        private async Task HoldOrPassBall(object arg)
        {
            Random rndChoice = new Random();
            int choice = rndChoice.Next(0, 1);
            if (choice == 0)
            {
                // How long should the Actor sleep?
                Random Sleep = new Random();
                int SleepTime = Sleep.Next(1, 100);
            }
            else
            {
                // Deciding which player should have a ball and decide what ball to receive

                Random rnd = new Random();
                int randomindex = rnd.Next(0, State.PlayerIds.Count);
                int randomballindex = rnd.Next(0, State.BallIds.Count);
                Guid randomplayer = State.PlayerIds[randomindex];
                Guid randomball = State.BallIds[randomballindex];
                IPlayer player = GrainFactory.GetGrain<IPlayer>(randomplayer);
                await player.ReceiveBall(randomball);
            }
        }

        /// <summary>
        /// If the player is now holding more than one ball, it enters a state in which it
        ///  passes all but the ball received to known other players selected at random.
        /// </summary>
        private async Task PassOtherBalls(object arg)
        {
            // Get to know whether the player has more than 1 ball
            Guid LatestBall = State.LatestBallReceived;

            foreach(Guid ball in State.BallIds)
            {
                if(LatestBall == ball)
                {
                    continue;
                }
                else
                {
                    Random rnd = new Random();
                    int randomindex = rnd.Next(0, State.PlayerIds.Count);
                    Guid randomplayer = State.PlayerIds[randomindex];
                    IPlayer player = GrainFactory.GetGrain<IPlayer>(randomplayer);
                    await player.ReceiveBall(ball);
                }
            }
        }
    }
}
