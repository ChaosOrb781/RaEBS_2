using GrainInterfaces;
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


    [StorageProvider(ProviderName = "OrleansStorage")]
    public class PlayerGrain : Grain<PlayerState>, IPlayer
    {
        private static Tuple<int, int> waitPeriod = Tuple.Create(500, 4000);

        public override Task OnActivateAsync()
        { 
            throw new NotImplementedException();
            //get last state of player before deactivation (from memory)
            //Give state to new actor
            //Initialize new Actor

        }

        public override Task OnDeactivateAsync() 
        {
            throw new NotImplementedException();
            //remember state when a player is deactivated
        }

        Task IPlayer.Initialize(List<Guid> playerIds, bool isHoldingBall)
        {
            foreach (Guid id in playerIds) {
                IPlayer player = GrainFactory.GetGrain<IPlayer>(id);
                //if {} Implemet whether or not the Actor has a state - if not, initialize otherwise go to isHoldingBall
                //playerIds.Add(id);
                if (isHoldingBall) {
                    //player.ReceiveBall();
                    State.BallIds = new List<Guid>();
                    State.BallIds.Add(new Guid());
                    // HoldOrPassBall(null); Maybe do not hold or pass ball here
                }
                else
                {
                    State.BallIds = new List<Guid>();
                    State.BallIds.Add(new Guid());
                    // player.ReceiveBall(new Guid()); Maybe the balls shouldn't be distributed here
                }
            }
            return Task.CompletedTask;
        }


               

        Task IPlayer.ReceiveBall(Guid ballId)
        {
            State.BallIds.Add(ballId);
            var i = State.BallIds.Count;
            if (i > 1)
            {
                PassOtherBalls(ballId);
            }
            else
            {
                HoldOrPassBall(ballId);
            }

            return null;

        }

        Task<List<Guid>> IPlayer.GetBallIds()
        {
            throw new NotImplementedException();
        }

        Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///The player waits for a random amount of time. When the time is up, 
        ///  the player then decides at random to either keep the ball for another
        ///  period or to pass the ball.
        /// </summary>
        private Task HoldOrPassBall(object arg)
        {
            Random rndChoice = new Random();
            int choice = rndChoice.Next(0, 1);
            if (choice == 0)
            {
                // How long should the Actor sleep?
                Random Sleep = new Random();
                int SleepTime = Sleep.Next();

                return null; // Bruh
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
                player.ReceiveBall(randomball);

                return null; //Bruh
                
            }
        }

        /// <summary>
        /// If the player is now holding more than one ball, it enters a state in which it
        ///  passes all but the ball received to known other players selected at random.
        /// </summary>
        private Task PassOtherBalls(object arg)
        {
            // Get to know whether the player has more than 1 ball
            Guid LatestBall = State.LatestBallReceived;

            foreach(Guid ball in State.BallIds)
            {
                if(LatestBall == ball)
                {
                    break;
                }
                else
                {
                    Random rnd = new Random();
                    int randomindex = rnd.Next(0, State.PlayerIds.Count);
                    Guid randomplayer = State.PlayerIds[randomindex];
                    IPlayer player = GrainFactory.GetGrain<IPlayer>(randomplayer);
                    player.ReceiveBall(ball);
                }
            }

            return null;
        }

    }
}
