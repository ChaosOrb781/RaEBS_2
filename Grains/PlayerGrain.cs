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
        public bool Marked = false;
        public StateSnapShot SnapShot = null;

        public void TakeSnapshot()
        {
            SnapShot = new StateSnapShot()
            {
                PlayerIds = this.PlayerIds,
                BallIds = this.BallIds,
                LatestBallReceived = this.LatestBallReceived
            };
        }

        public void ResetMarking()
        {
            Marked = false;
            SnapShot = null;
        }
    }


    [StorageProvider]
    public class PlayerGrain : Grain<PlayerState>, IPlayer
    {
        public override async Task OnActivateAsync()
        {
            //During custom games, the player id has yet to be fetched
            //Print("Activated");
            await ReadStateAsync(); //Should be called on activation automatically
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync() 
        {
            Print("Deactivated");
            await WriteStateAsync(); //Should be called on deactivation automatically
            await base.OnDeactivateAsync();
        }

        public async Task Initialize(IEnumerable<Guid> playerIds, bool isHoldingBall)
        {
            //No ReadStateAsync as we want to override existing
            await ReadStateAsync();
            
            State.PlayerIds = playerIds.ToList();
            State.BallIds = new List<Guid>();
            State.ResetMarking();
            if (isHoldingBall)
            {
                Guid newBall = Guid.NewGuid();
                State.BallIds.Add(newBall);
                State.LatestBallReceived = newBall;
            }
            Print("Initialized and {0} a ball", isHoldingBall ? "has" : "does not have");
            StartOrRestartPass();
            await WriteStateAsync();
        }

        public async Task<bool> ReceiveBall(Guid ballId)
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId); //Write to state
            State.LatestBallReceived = ballId; // Write to state
            Print("Received ball {0}", (Statics.Values.Balls.ToList().IndexOf(ballId) == -1) ? "" + ballId : "" + (Statics.Values.Balls.ToList().IndexOf(ballId) + 1));
            await WriteStateAsync(); //Save state
            return State.Marked;
        }   

        public async Task<List<Guid>> GetBallIds()
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            Print("Someone is inspecting my balls");
            return this.State.BallIds;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            Print("Reminder received I have: {0}", BallListToString(State.BallIds));
            switch (reminderName)
            {
                case Statics.Values.PassReminderName:
                    if (State.BallIds.Count == 1)
                    {
                        await HoldOrPassBall();
                    } 
                    else if (State.BallIds.Count > 1) 
                    {
                        await PassOtherBalls();
                        await HoldOrPassBall();
                    }
                    StartOrRestartPass();
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
            int tossChoice = Statics.Values.Randomizer.Next(Statics.Values.MinChange, Statics.Values.MaxChance);
            if (tossChoice <= Statics.Values.TossChange)
            {
                int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 1);
                while (otherPlayerIndex == State.PlayerIds.IndexOf(this.GetPrimaryKey()))
                {
                    Print("HoldOrPass, accidentally chose myself, choosing other");
                    otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 1);
                }

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                Print("HoldOrPass, tossing ball {0} to player {1}", Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) == -1 ? ""+State.LatestBallReceived : ""+(Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) + 1), otherPlayerIndex + 1);

                bool debug_prevmark = State.Marked;
                //Always keep updated snapshot in memory 
                if (!State.Marked)
                {
                    State.TakeSnapshot();
                }
                State.Marked = await otherPlayer.ReceiveBall(State.LatestBallReceived);

                State.BallIds.Remove(State.LatestBallReceived);

                if (State.Marked != debug_prevmark)
                {
                    Print("HoldOrPass, I HAVE BEEN INFECTED!");
                }
            }
            await WriteStateAsync();
        }

        private async Task PassOtherBalls()
        {
            await ReadStateAsync();
            Print("PassOtherBalls, started and my latest received {0}", Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) == -1 ? "" + State.LatestBallReceived : "" + (Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) + 1));

            foreach (Guid otherBall in new List<Guid>(State.BallIds)) {
                Print("Torsing ball {0}", (Statics.Values.Balls.ToList().IndexOf(otherBall) == -1) ? "" + otherBall : "" + (Statics.Values.Balls.ToList().IndexOf(otherBall) + 1));
                if (State.LatestBallReceived == otherBall)
                {
                    Print("Skipping ball {0} since it was equal to ball {1}", (Statics.Values.Balls.ToList().IndexOf(otherBall) == -1) ? "" + otherBall : "" + (Statics.Values.Balls.ToList().IndexOf(otherBall) + 1), Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) == -1 ? "" + State.LatestBallReceived : "" + (Statics.Values.Balls.ToList().IndexOf(State.LatestBallReceived) + 1));
                    continue;
                }

                int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 1);
                while (otherPlayerIndex == State.PlayerIds.IndexOf(this.GetPrimaryKey()))
                {
                    Print("HoldOrPass, accidentally chose myself, choosing other");
                    otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 1);
                }

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                Print("PassOtherBalls, tossing ball {0} to player {1}", (Statics.Values.Balls.ToList().IndexOf(otherBall) == -1) ? "" + otherBall : "" + (Statics.Values.Balls.ToList().IndexOf(otherBall) + 1), otherPlayerIndex + 1);

                bool debug_prevmark = State.Marked;
                //Always keep updated snapshot in memory 
                if (!State.Marked)
                {
                    State.TakeSnapshot();
                }

                State.Marked = await otherPlayer.ReceiveBall(otherBall);
                State.BallIds.Remove(otherBall);

                if (State.Marked != debug_prevmark)
                {
                    Print("PassOtherBalls, I HAVE BEEN INFECTED!");
                }
            }
            Print("PassOtherBalls, has {0} balls remaining after tosses", State.BallIds.Count);
            //If some asynchronous task dealt the latestball out (such as hold or receive) then set first as most recent
            if (!State.BallIds.Contains(State.LatestBallReceived))
            {
                Print("PassOtherBalls, LatestBallReceived has been passed (unintended), setting new latest received");
                State.LatestBallReceived = State.BallIds.FirstOrDefault();
            }

            await WriteStateAsync();
        }

        private async void StartOrRestartPass()
        {
            await this.RegisterOrUpdateReminder(Statics.Values.PassReminderName,
                TimeSpan.FromSeconds(Statics.Values.Randomizer.Next(Statics.Values.WaitTimeMin, Statics.Values.WaitTimeMax)),
                //TimeSpan.FromSeconds(Statics.Values.WaitTimeMin),
                TimeSpan.FromSeconds(Statics.Values.MaxWaitReminderTime));
        }

        public async Task PrimaryMark()
        {
            await ReadStateAsync();
            Print("I have become the chosen one for primary snapshot");
            State.Marked = true;
            State.TakeSnapshot();
            await WriteStateAsync();
            foreach (Guid otherId in State.PlayerIds)
            {
                if (this.GetPrimaryKey() == otherId)
                    continue;
                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(otherId);
                await otherPlayer.Mark();
            }
        }

        public async Task Mark()
        {
            await ReadStateAsync();
            Print(State.Marked ? "Attempted to remark, ignoring" : "I have been marked");
            if (!State.Marked) {
                State.Marked = true;
                State.TakeSnapshot();
            }
            await WriteStateAsync();
        }

        public async Task<bool> IsMarked()
        {
            await ReadStateAsync();
            return State.Marked;
        }

        public async Task<StateSnapShot> GetSnapShot()
        {
            await ReadStateAsync();
            return State.SnapShot;
        }

        private void Print(string message, params object[] args)
        {
            Console.WriteLine("Player " + (State.PlayerIds.ToList().IndexOf(this.GetPrimaryKey()) + 1) + ": " + message, args);
        }

        private string BallListToString(List<Guid> list)
        {
            string s = "";
            if (list.Count > 0)
            {
                if (Statics.Values.Balls.Contains(list[0]))
                {
                    s += "[";
                    if (list.Count > 0)
                    {
                        foreach (Guid o in list)
                        {
                            s += (Statics.Values.Balls.ToList().IndexOf(o) + 1) + ", ";
                        }
                        s = s.Substring(0, s.Length - 1);
                    }
                    s += "]";
                } 
                else
                {
                    s += "# of balls: " + list.Count;
                }
            }
            return s;
        }
    }
}