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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
            // Await that others receive balls
        }

        /// <summary>
        /// If the player is now holding more than one ball, it enters a state in which it
        ///  passes all but the ball received to known other players selected at random.
        /// </summary>
        private Task PassOtherBalls(object arg)
        {
            throw new NotImplementedException();
        }

    }
}
