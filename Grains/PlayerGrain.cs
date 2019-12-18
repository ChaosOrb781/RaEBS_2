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
        public Guid LatestBallReceived { set; get; } = Guid.Empty;
    }


    [StorageProvider]
    public class PlayerGrain : Grain<PlayerState>, IPlayer
    {

        private IDisposable Reminder = null;

        public async override Task OnActivateAsync()
        {
            //ReadStateAsync(); //Should be called on activation automatically
            await ReadStateAsync();
            Console.WriteLine("Now we are activating {0}", this.GetPrimaryKey());
            await base.OnActivateAsync();

        }

        public async override Task OnDeactivateAsync()
        {
            //WriteStateAsync(); //Should be called on deactivation automatically
            await WriteStateAsync();
            Console.WriteLine("Now we are DEactivating {0}", this.GetPrimaryKey());
            await base.OnDeactivateAsync();
        }

        public async Task Initialize(IEnumerable<Guid> playerIds, bool isHoldingBall)
        {
            //No ReadStateAsync as we want to override existing
            await ReadStateAsync();
            
            State.PlayerIds = playerIds.ToList();
            State.BallIds = new List<Guid>();
            if (isHoldingBall)
            {
                State.BallIds.Add(Guid.NewGuid());
            }
            Console.WriteLine("Now we initializing {0}", this.GetPrimaryKey());
            await WriteStateAsync();
        }


        


        // At the start of the game, the players will be given a ball and wait for the game to start
        public async Task GiveBallToPlayer(Guid ballId)
        {
            State.BallIds.Add(ballId);
            Console.WriteLine("GiveBallToPlayer _ Player {0} has been given ball {1}", this.GetPrimaryKey(), ballId);

            await WriteStateAsync();
            await Task.CompletedTask;
        }


        public async Task ReceiveBall(Guid ballId)
        {

            //await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            State.BallIds.Add(ballId);

            Console.WriteLine("RECEIVE BALL _ Player {0} received ball {1}", this.GetPrimaryKey(), ballId);

            await WriteStateAsync();

            await Task.CompletedTask;

        }



        public async Task<List<Guid>> GetBallIds()
        {
            //await ReadStateAsync(); //Update state; Should be unnecessary as recovered OnActivation
            await ReadStateAsync();
            return this.State.BallIds;
        }



        /*
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
        */

        public async Task HoldOrPassBallTruelyRandom(object arg)
        {
            await ReadStateAsync();

            Guid ballId = Guid.Parse(arg.ToString());
            Random Randomizer = new Random();


            if (State.BallIds.Count > 1)
            {
                await PassOtherBallsTruelyRandom(ballId);
            }

            if (Convert.ToBoolean(Randomizer.Next(0, 2)))
            {
               
                //Check from 0 to N - 2 (removing this player from the list)
                int otherPlayerIndex = Randomizer.Next(0, State.PlayerIds.Count - 2);

                //If this player's id was chosen, just add one by the logic of previous 
                otherPlayerIndex = (otherPlayerIndex >= State.PlayerIds.IndexOf(this.GetPrimaryKey())) ? otherPlayerIndex++ : otherPlayerIndex;

                IPlayer otherPlayer = GrainFactory.GetGrain<IPlayer>(State.PlayerIds[otherPlayerIndex]);


                Console.WriteLine("HOLD OR PASS _ Player {0, 32} threw ball {1, 32} to player {2, 32}", this.GetPrimaryKey(), ballId, otherPlayer.GetPrimaryKey());


                State.BallIds.Remove(ballId);

                await otherPlayer.ReceiveBall(ballId);

                await WriteStateAsync();

                // Deactivate the fucking player when he has no balls (lel)
                await OnDeactivateAsync();

                // Await Task.CompletedTask;
            }
            
            
            // Console.WriteLine("Timer! _1_  (Hold or Pass ball)");
            // Dispose the former reminder if there is one
            if(Reminder != null)
            {
                Reminder.Dispose();
            }
            // Otherwise set new reminder
            Reminder = RegisterTimer(HoldOrPassBallTruelyRandom, (Guid)ballId, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(5100));
            // Await Task.CompletedTask;

            await Task.CompletedTask;
        }





        private async Task PassOtherBallsTruelyRandom(Guid ballId)
        {
            await ReadStateAsync();

            Random Randomizer = new Random();


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

