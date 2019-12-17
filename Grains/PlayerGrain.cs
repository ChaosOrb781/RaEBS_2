using GrainInterfaces;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Grains
{
    public class PlayerState
    {
        public List<Guid> PlayerIds { set; get; } = new List<Guid>();
        public List<Guid> BallIds { set; get; } = new List<Guid>();
        public Guid LatestBallReceived = Guid.Empty;
    }


    [StorageProvider]
    public class PlayerGrain : Grain<PlayerState>, IPlayer
    {
        public override async Task OnActivateAsync()
        {
            await ReadStateAsync(); //Should be called on activation automatically
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync() 
        {
            await WriteStateAsync(); //Should be called on deactivation automatically
            await base.OnDeactivateAsync();
        }

        public Task Initialize(IEnumerable<Guid> playerIds, bool isHoldingBall)
        {
            //No ReadStateAsync as we want to override existing
            State.PlayerIds = playerIds.ToList();
            State.BallIds = new List<Guid>();
            if (isHoldingBall)
            {
                Guid newBall = Guid.NewGuid();
                State.BallIds.Add(newBall);
                State.LatestBallReceived = newBall;
            }
            StartOrRestartHoldOrPass();
            StartOrRestartPassOtherBalls();
            return WriteStateAsync();
        }

        public async Task ReceiveBall(Guid ballId)
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId); //Write to state
            State.LatestBallReceived = ballId; // Write to state
            await WriteStateAsync(); //Save state
        }   

        public async Task<List<Guid>> GetBallIds()
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            return this.State.BallIds;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case Statics.Values.TossReminderName:
                    await HoldOrPassBall();
                    StartOrRestartHoldOrPass();
                    break;
                case Statics.Values.PassReminderName:
                    await PassOtherBalls();
                    StartOrRestartPassOtherBalls();
                    break;
                default:
                    break;
            }
            if (State.BallIds.Count == 0)
                await OnDeactivateAsync();
        }

        private async Task HoldOrPassBall()
        {
            await ReadStateAsync();
            //Cannot toss if no ball
            if (State.BallIds.Count < 1)
                return;
            else if (State.BallIds.Count > 1)
                await PassOtherBalls();
            int tossChoice = Statics.Values.Randomizer.Next(Statics.Values.MinChange, Statics.Values.MaxChance);
            if (tossChoice <= Statics.Values.TossChange)
            {
                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 2);
                //If this player's id was chosen, just add one by the logic of previous line
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                await otherPlayer.ReceiveBall(State.LatestBallReceived);
                State.BallIds.Remove(State.LatestBallReceived);
                await WriteStateAsync();
            }
        }

        private async Task PassOtherBalls()
        {
            await ReadStateAsync();
            while (State.BallIds.Count > 1)
            {
                //If lowest ball in stack is Latest, skip that and toss others
                int index = (State.BallIds[0] == State.LatestBallReceived) ? 1 : 0;
                Guid ball = State.BallIds[index];

                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 2);
                //If this player's id was chosen, just add one by the logic of previous line
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                await otherPlayer.ReceiveBall(ball);
                State.BallIds.Remove(ball);
            }
            //If some asynchronous task dealt the latestball out (such as hold or receive) then set first as most recent
            if (!State.BallIds.Contains(State.LatestBallReceived))
                State.LatestBallReceived = State.BallIds.FirstOrDefault();

            await WriteStateAsync();
        }

        private async void StartOrRestartHoldOrPass()
        {
            await this.RegisterOrUpdateReminder(Statics.Values.TossReminderName,
                TimeSpan.FromSeconds(Statics.Values.Randomizer.Next(Statics.Values.WaitTimeMin, Statics.Values.WaitTimeMax)),
                TimeSpan.FromSeconds(Statics.Values.MaxWaitReminderTime));
        }
        private async void StartOrRestartPassOtherBalls()
        {
            await this.RegisterOrUpdateReminder(Statics.Values.PassReminderName,
                TimeSpan.FromSeconds(Statics.Values.WaitTimeMin),
                TimeSpan.FromSeconds(Statics.Values.MaxWaitReminderTime));
        }

    }
}
