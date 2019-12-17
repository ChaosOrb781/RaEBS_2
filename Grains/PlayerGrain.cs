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
        public override Task OnActivateAsync()
        {
            //ReadStateAsync(); //Should be called on activation automatically
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync() 
        {
            //WriteStateAsync(); //Should be called on deactivation automatically
            return base.OnDeactivateAsync();
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
            return WriteStateAsync();
        }

        public async Task ReceiveBall(Guid ballId)
        {
            //await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId); //Write to state
            State.LatestBallReceived = ballId; // Write to state
            await WriteStateAsync(); //Save state
            await PassOtherBallsTruelyRandom(); //Pass all other balls but latest
            await HoldOrPassBallTruelyRandom(); //Decide if we keep latest
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
                    GrainFactory.GetGrain<IPlayer>(this.GetPrimaryKey());
                    await HoldOrPassBallTruelyRandom();
                    break;
                default:
                    break;
            }
            this.UnregisterReminder(await GetReminder(reminderName)).Dispose();
        }

        private async Task HoldOrPassBallTruelyRandom()
        {
            //Cannot toss if no ball
            if (State.BallIds.Count == 0)
                return;
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
            else
            {
                RandomWait();
            }
        }

        private async Task HoldOrPassBallLogicRandom()
        {
            //Cannot toss if no ball
            if (State.BallIds.Count == 0)
                return;
            int tossChoice = Statics.Values.Randomizer.Next(Statics.Values.MinChange, Statics.Values.MaxChance);
            if (tossChoice <= Statics.Values.TossChange)
            {
                //Only toss to player who can receive, this is not necessarily thread-safe,
                //many actors can toss to same actor if their count is observed to be 0 balls
                //Hence we "PassOtherBalls" before we "HoldOrPassBall"
                IPlayer otherPlayer = null;
                List<Guid> balls = new List<Guid>();
                do
                {
                    //Check from 0 to N - 2 (removing this player from the list)
                    int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 2);
                    //If this player's id was chosen, just add one by the logic of previous line
                    otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                    otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);
                    //Tickle their balls a little to see if they have any
                    balls = await otherPlayer.GetBallIds();
                } while (balls.Count > 0 && otherPlayer != null);

                await otherPlayer.ReceiveBall(State.LatestBallReceived);
                State.BallIds.Remove(State.LatestBallReceived);
                await WriteStateAsync();
            }
            else
            {
                RandomWait();
            }
        }

        private async Task PassOtherBallsTruelyRandom()
        {
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
                await WriteStateAsync();
            }
        }

        private async Task PassOtherBallsLogicRandom()
        {
            while (State.BallIds.Count > 1)
            {
                //If lowest ball in stack is Latest, skip that and toss others
                int index = (State.BallIds[0] == State.LatestBallReceived) ? 1 : 0;
                Guid ball = State.BallIds[index];

                //Only toss to player who can receive, this is not necessarily thread-safe,
                //many actors can torse to same actor if their count is observed to be 0 balls
                //Hence we "PassOtherBalls" before we "HoldOrPassBall"
                IPlayer otherPlayer = null;
                List<Guid> balls = new List<Guid>();
                do
                {
                    //Check from 0 to N - 2 (removing this player from the list)
                    int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 2);
                    //If this player's id was chosen, just add one by the logic of previous line
                    otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                    otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);
                    //Tickle their balls a little to see if they have any
                    balls = await otherPlayer.GetBallIds();
                } while (balls.Count > 0 && otherPlayer != null);

                await otherPlayer.ReceiveBall(ball);
                State.BallIds.Remove(ball);
                await WriteStateAsync();
            }
        }

        private async void RandomWait()
        {
            IGrainReminder reminderRegistration = await this.RegisterOrUpdateReminder(
                Statics.Values.TossReminderName,
                TimeSpan.FromMinutes(1),    //The amount of time to delay before firing the reminder
                TimeSpan.FromMinutes(2));    //The time interval between firing of reminders
            /*
            int timeWaitSeconds = Statics.Values.Randomizer.Next(Statics.Values.WaitTimeMin, Statics.Values.WaitTimeMax);
            //?? Dont use await to prevent blocking actions!
            //Might just be registering it instead of waiting, if so do await
            await this.RegisterOrUpdateReminder(Statics.Values.TossReminderName,
                TimeSpan.FromSeconds(Statics.Values.WaitTimeMin),
                TimeSpan.FromSeconds(Statics.Values.WaitTimeMin - 1));
            */
        }

    }
}
