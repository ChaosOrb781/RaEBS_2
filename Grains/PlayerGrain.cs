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
        public List<object> MessagesReceived { set; get; } = new List<object>();
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
                State.BallIds.Add(Guid.NewGuid());
            }
            return WriteStateAsync();
        }




        public async Task ReceiveBall(Guid ballId)
        {
            Console.WriteLine("Player {0} received ball {1}", this.GetPrimaryKey(), ballId);
            //await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId);

            // Pass all other balls but latest
            await PassOtherBallsTruelyRandom(ballId);

            // Get Snapshot
            await WriteStateAsync();

            await InitializeSnapshot(this.GetPrimaryKey());


            // Set Timer
            RegisterTimer(HoldOrPassBallTruelyRandom, ballId, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            //await HoldOrPassBallTruelyRandom(); //Decide if we keep latest
        }



        public Task<List<Guid>> GetBallIds()
        {
            //await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            return Task.FromResult(this.State.BallIds);
        }




        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case Statics.Values.TossReminderName:
                    GrainFactory.GetGrain<IPlayer>(this.GetPrimaryKey());
                    //await HoldOrPassBallTruelyRandom();
                    break;
                default:
                    break;
            }
        }

        private async Task HoldOrPassBallTruelyRandom(object arg)
        {
            Guid ballId = Guid.Parse(arg.ToString());

            Random Randomizer = new Random();

            //Cannot Toss if no ball
            //if (State.BallIds.Count == 0)
            //    return;

            if (State.BallIds.Count > 1)
            {
                await PassOtherBallsTruelyRandom(ballId);
            }

            if (Convert.ToBoolean(Randomizer.Next(0, 2)))
            {
               
                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Randomizer.Next(0, State.PlayerIds.Count - 1);

                //If this player's id was chosen, just add one by the logic of previous 
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);


                await otherPlayer.ReceiveBall(ballId);
                State.BallIds.Remove(ballId);

                Console.WriteLine("Player {0} threw ball {1} to player {2}", this.GetPrimaryKey(), ballId, otherPlayer.GetPrimaryKey());



                await WriteStateAsync();
            }
            else
            {
                
                RegisterTimer(HoldOrPassBallTruelyRandom, ballId, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            }
        }
        /*
        private async Task HoldOrPassBallLogicRandom(Guid ballId)
        {
            //Cannot torse if no ball
            if (State.BallIds.Count == 0)
                return;
            int torseChoice = Statics.Values.Randomizer.Next(Statics.Values.MinChange, Statics.Values.MaxChance);
            if (torseChoice <= Statics.Values.TossChange)
            {
                //Only torse to player who can receive, this is not necessarily thread-safe,
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

                await otherPlayer.ReceiveBall(ballId);
                State.BallIds.Remove(ballId);
                await WriteStateAsync();
            }
            else
            {
                int timeWaitMilliseconds = Statics.Values.Randomizer.Next(Statics.Values.WaitTimeMin, Statics.Values.WaitTimeMax);
                //?? Dont use await to prevent blocking actions!
                //Might just be registering it instead of waiting, if so do await
                await this.RegisterOrUpdateReminder("RemindToPass", 
                    TimeSpan.FromMilliseconds(Statics.Values.WaitTimeMin), 
                    TimeSpan.FromMilliseconds(timeWaitMilliseconds));
            }
        }
        */

        private async Task PassOtherBallsTruelyRandom(Guid ballId)
        {
            Random Randomizer = new Random();

            while (State.BallIds.Count > 1)
            {
                foreach (Guid ball in State.BallIds)
                {
                    if (ball == ballId)
                    {
                        continue;
                    }
                    //Check from 0 to N - 2 (removing this player from the list)
                    int otherPlayerIndex = Randomizer.Next(0, State.PlayerIds.Count - 1);
                    //If this player's id was chosen, just add one by the logic of previous line
                    otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;
                    IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);

                    Console.WriteLine("Player {0} threw ball {1} to player {2}", this.GetPrimaryKey(), ball, otherPlayer.GetPrimaryKey());
                    await otherPlayer.ReceiveBall(ball);
                    State.BallIds.Remove(ball);
                    await WriteStateAsync();

                    // Maybe
                    await HoldOrPassBallTruelyRandom(ball);
                }


            }
            
        }






        // We want to figure out who has which balls

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

            object message = ("Player {0} has ball(s) {1}", ID, BallIDs[0]);

            return message;
        }

        /*
        // We also want to keep track of the messages that the player "Receives" - meaning all the messages on its incoming channel
        // Every Actor "knows about" N-1 other Actors
        public async void MessagesReceived(Guid Initialplayer, object message)
        {

            return;
        }
        */


    }
}

