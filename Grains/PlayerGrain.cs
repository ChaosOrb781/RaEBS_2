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
            await ReadStateAsync(); //Should be called on activation automatically
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync() 
        {
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
            StartOrRestartPass();
            return WriteStateAsync();
        }

        public async Task<bool> ReceiveBall(Guid ballId)
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId); //Write to state
            State.LatestBallReceived = ballId; // Write to state
            await WriteStateAsync(); //Save state
            return State.Marked;
        }   

        public async Task<List<Guid>> GetBallIds()
        {
            await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            return this.State.BallIds;
        }



        /*
        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case Statics.Values.PassReminderName:
                    await HoldOrPassBall();
                    StartOrRestartPass();
                    break;
                default:
                    break;
            }
            if (State.BallIds.Count == 0)
                await OnDeactivateAsync();
        }
        */

        private async Task HoldOrPassBall()
        {
            await ReadStateAsync();
            //Cannot toss if no ball
            if (State.BallIds.Count < 1)
                return;
            else if (State.BallIds.Count > 1)
                await PassOtherBalls();
            await ReadStateAsync();
            int tossChoice = Statics.Values.Randomizer.Next(Statics.Values.MinChange, Statics.Values.MaxChance);
            if (tossChoice <= Statics.Values.TossChange)
            {
                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Statics.Values.Randomizer.Next(0, State.PlayerIds.Count - 2);
                //If this player's id was chosen, just add one by the logic of previous line
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                //Always keep updated snapshot in memory 
                if (!State.Marked)
                {
                    State.TakeSnapshot();
                }
                State.Marked = await otherPlayer.ReceiveBall(State.LatestBallReceived);

                State.BallIds.Remove(State.LatestBallReceived);
            }
            await WriteStateAsync();
        }

        private async Task PassOtherBalls()
        {
            await ReadStateAsync();
            while (State.BallIds.Count > 1)
            {
                //If lowest ball in stack is Latest, skip that and toss others
                //Non descructive if LatestBallReceived is not part of the list
                int index = (State.BallIds[0] == State.LatestBallReceived) ? 1 : 0;
                Guid ball = State.BallIds[index];

                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Randomizer.Next(0, State.PlayerIds.Count - 2);

                //If this player's id was chosen, just add one by the logic of previous 
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                //Always keep updated snapshot in memory 
                if (!State.Marked)
                {
                    State.TakeSnapshot();
                }
                State.Marked = await otherPlayer.ReceiveBall(ball);
                State.BallIds.Remove(ball);
            }
            //If some asynchronous task dealt the latestball out (such as hold or receive) then set first as most recent
            if (!State.BallIds.Contains(State.LatestBallReceived))
                State.LatestBallReceived = State.BallIds.FirstOrDefault();

            await WriteStateAsync();
        }

        private async void StartOrRestartPass()
        {
            await this.RegisterOrUpdateReminder(Statics.Values.PassReminderName,
                //TimeSpan.FromSeconds(Statics.Values.Randomizer.Next(Statics.Values.WaitTimeMin, Statics.Values.WaitTimeMax)),
                TimeSpan.FromSeconds(Statics.Values.WaitTimeMin),
                TimeSpan.FromSeconds(Statics.Values.MaxWaitReminderTime));
        }

        public async Task PrimaryMark()
        {
            await ReadStateAsync();
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
            if (State.Marked) {
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
    }
}

            foreach (Guid ball in State.BallIds)
            {
                if (ball == ballId)
                {
                    continue;
                }
                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Randomizer.Next(0, State.PlayerIds.Count - 2);
                //If this player's id was chosen, just add one by the logic of previous line
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                State.BallIds.Remove(ball);

                Console.WriteLine("PASS OTHER _ Player {0} threw ball {1} to player {2}", this.GetPrimaryKey(), ball, otherPlayer.GetPrimaryKey());
                await otherPlayer.ReceiveBall(ball);


                await WriteStateAsync();
            }

            await Task.CompletedTask;
        }






        // We want to figure out who has which 
        // First we want to initialize a snapshot if the player has not sent a "Snapshot" of what balls he has
        public async Task InitializeSnapshot(Guid FromPlayerID)
        {
            

            Guid thisPlayer = GrainFactory.GetGrain<IPlayer>(FromPlayerID).GetPrimaryKey();

            List<Guid> Players = State.PlayerIds;
            foreach (Guid player in Players)
            {
                if (player != thisPlayer)
                {
                    object message = this.SendMarkerMessage(player);
                    bool alreadyExists = State.MessagesReceived.Contains(message);
                    if (!alreadyExists)
                    {
                        State.MessagesReceived.Add(message);
                        Console.WriteLine("Message Received for player {0}: {1}", player ,message);
                    }
                }
                else
                {
                    continue;
                }
            }
            return;
        }

        // Right after initializing the Snapshot, the player has to send a marker message out of each of its outgoing channels
        // So, an actor - in this case a player - is playing with 10 other players, the player has 10 outgoing channels and 10 incoming channels
        public async Task<object> SendMarkerMessage(Guid player)
        {
            

            IPlayer ReceivingPlayer = GrainFactory.GetGrain<IPlayer>(player);

            //Console.WriteLine("Getting the Guid of the player");
            Guid ID = ReceivingPlayer.GetPrimaryKey();

            //Console.WriteLine("Getting ball(s) of the player");
            List<Guid> BallIDs = await ReceivingPlayer.GetBallIds();


            /*
            for(int i = 0; i < BallIDs.Count; i++)
            {
                Console.WriteLine("Player {0} has ball(s) {1}", ID, BallIDs[i]);
            }
            */

            object message = ("Player (Channel) {0} has ball(s) {1}", ID, BallIDs[0]);

            return message;
        }

        /*
        // We also want to keep track of the messages that the player "Receives" - meaning all the messages on its incoming channel
        // Every Actor "knows about" N-1 other Actors. This is implemented in the above functions
        public async void MessagesReceived(Guid Initialplayer, object message)
        {

            return;
        }
        */


    }
}

