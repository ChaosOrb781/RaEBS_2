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
        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
            //throw new NotImplementedException();
        }

        public override Task OnDeactivateAsync() 
        {
            return base.OnDeactivateAsync();
            //throw new NotImplementedException();
        }

        Task IPlayer.Initialize(List<Guid> playerIds, bool isHoldingBall)
        {

            State.PlayerIds = playerIds;
            State.BallIds = new List<Guid>();
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        Task IPlayer.ReceiveBall(Guid ballId)
        {
            State.BallIds.Add(ballId);

            //WriteStateAsync Writes to the database!!!
            this.WriteStateAsync();

            //Pass all other balls than the ballID in the following argument
            this.PassOtherBalls(ballId);

            this.WriteStateAsync();

            //WriteStateAsync Updates the database!!!
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }   

        async Task<List<Guid>> IPlayer.GetBallIds()
        {
            //ReadStateAsync reads whatever it says in the database:
            await this.ReadStateAsync();
            return this.State.BallIds;
            //throw new NotImplementedException();
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
        }


        /// <summary>
        /// If the player is now holding more than one ball, it enters a state in which it
        ///  passes all but the ball received to known other players selected at random.
        /// </summary>
        async private Task PassOtherBalls(object arg)
        {
            // Find random player to throw ball to
            //Already hass the ball the player wants to hold on to through Receiveball()
            var allBalls = State.BallIds;
            

            //throw new NotImplementedException();
        }

    }
}
