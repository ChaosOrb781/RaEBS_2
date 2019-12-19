using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace OrleansBasics
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();

                var client = await ConnectClient();

                Console.WriteLine("\n\n Press Enter to start process...\n\n");
                Console.ReadLine();

                bool runStaticExample = false;
                while (true)
                {
                    Console.Write("Run custom game (Y) or static game with integer enumerated players (spawns 10)(N): ");
                    string input = Console.ReadLine();
                    if (input.ToLower() == "y") {
                        runStaticExample = false;
                        break;
                    }
                    if (input.ToLower() == "n")
                    {
                        runStaticExample = true;
                        break;
                    }
                    Console.WriteLine("Invalid answer, expected Y or N");
                }

                List<Guid> players = new List<Guid>(Statics.Values.Players);
                List<Guid> balls = new List<Guid>(Statics.Values.Balls);
                int amountOfPlayers = 10, amountOfBalls = 9;
                if (!runStaticExample)
                {
                    while (true)
                    {
                        Console.Write("Enter amount of players (N): ");
                        string input = Console.ReadLine();
                        bool success = Int32.TryParse(input, out amountOfPlayers);
                        if (success)
                            break;
                        else
                            Console.WriteLine("Invalid player amount, try again");
                    }

                    while (true)
                    {
                        Console.Write("Enter amount of balls (K, K < N): ");
                        string input = Console.ReadLine();
                        bool success = Int32.TryParse(input, out amountOfBalls);
                        if (success && amountOfBalls < amountOfPlayers)
                            break;
                        else
                            Console.WriteLine("Invalid ball amount, needs to be less than players, try again");
                    }

                    players.Clear();
                    balls.Clear();

                    for(int i = 0; i < amountOfPlayers; i++)
                        players.Add(Guid.NewGuid());
                    for (int i = 0; i < amountOfBalls; i++)
                        balls.Add(Guid.NewGuid());
                }

                Console.WriteLine("Initializing players with no balls...");
                List<Task> initialize = new List<Task>();
                for (int i = 0; i < amountOfPlayers; i++)
                {
                    IPlayer player = client.GetGrain<IPlayer>(players[i]);
                    initialize.Add(player.Initialize(players, false));
                }
                await Task.WhenAll(initialize);

                Console.WriteLine("Giving all balls to the random players...");
                List<Task> balltosses = new List<Task>();
                for (int i = 0; i < amountOfBalls; i++)
                {
                    IPlayer player = client.GetGrain<IPlayer>(players[Statics.Values.Randomizer.Next(0,amountOfBalls - 1)]);
                    balltosses.Add(player.ReceiveBall(balls[i]));
                }
                await Task.WhenAll(balltosses);

                Console.WriteLine("Press enter to take snapshot (at any point!)...");
                Console.ReadLine();

                IPlayer player1 = client.GetGrain<IPlayer>(players[0]);
                await player1.PrimaryMark();

                bool allmarked;
                do
                {
                    allmarked = true;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    foreach (Guid playerid in players)
                    {
                        IPlayer player = client.GetGrain<IPlayer>(playerid);
                        allmarked &= await player.IsMarked();
                    }
                } while (!allmarked);

                List<Task<StateSnapShot>> allSnapshots = new List<Task<StateSnapShot>>();
                foreach (Guid playerid in players)
                {
                    IPlayer player = client.GetGrain<IPlayer>(playerid);
                    allSnapshots.Add(player.GetSnapShot());
                }
                await Task.WhenAll(allSnapshots);

                //Check immidially after
                int numBalls = 0;
                for (int i = 0; i < amountOfPlayers; i++)
                {
                    numBalls += allSnapshots[i].Result.BallIds.Count;
                    if (runStaticExample)
                        Console.WriteLine("Player {0} had {1} balls", i + 1, allSnapshots[i].Result.BallIds.Count);
                }

                await host.StopAsync();

                Console.WriteLine("Players had {0} balls, expected {1}, validates: {2}", numBalls, amountOfBalls, numBalls == amountOfBalls);

                Console.WriteLine("Press to terminate program...");
                Console.ReadLine();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(PlayerGrain).Assembly).WithReferences())
                //.ConfigureLogging(logging => logging.AddConsole())
                .AddAdoNetGrainStorageAsDefault(options =>
                {
                    options.Invariant = "Npgsql";
                    options.ConnectionString = "host=localhost;database=OrleansStorage;password=postgres;username=postgres";
                    options.UseJsonFormat = true;
                })
               .UseInMemoryReminderService(); 

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }


        private static async Task<IClusterClient> ConnectClient()
        {
            IClusterClient client;
            client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                //.ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }


        static List<Guid> SpawnPlayers(IClusterClient client, int number_of_players)
        {
            List<Guid> ListID = new List<Guid>();
            for (int i = 0; i < number_of_players; i++)
            {
                var player = client.GetGrain<IPlayer>(Guid.NewGuid());
                ListID.Add(player.GetPrimaryKey());
            }

            for (int i = 0; i < number_of_players; i++)
            {
                var player = client.GetGrain<IPlayer>(ListID[i]);
                player.Initialize(ListID, false);
            }

            Console.WriteLine("PLAYERS: \n\n");
            for (int i = 0; i < number_of_players; i++)
            {
                Console.WriteLine(ListID[i]);
            }
            return ListID;

        }


        private static async Task GiveRandomPlayersBalls(IClusterClient client, List<Guid> AllPlayers, int number_of_balls)
        { 
            Random rng = new Random();

            List<Guid> ballsList = new List<Guid>();

            int balls = number_of_balls;

            // Returns the same ball -- goddammit
            for (int i = 0; i < number_of_balls; i++)
            {
                ballsList.Add(Guid.NewGuid());
            }

            // Print the balls added to the game
            Console.WriteLine("\n\n BALLS :");
            for (int i = 0; i < balls; i++)
            {
                Console.WriteLine("{0}", ballsList[i]);
            }

            // Shuffle list of players randomly to and give K balls to the first K players
            int n = AllPlayers.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Guid value = AllPlayers[k];
                AllPlayers[k] = AllPlayers[n];
                AllPlayers[n] = value;
            }

            Console.WriteLine("\n\n Shuffled List of Players : ");

            for (int i = 0; i < AllPlayers.Count; i++)
            {
                Console.WriteLine(AllPlayers[i]);
            }

            Console.WriteLine("\n\nAmount of balls {0}", ballsList.Count);

            // K balls
            for (int i = 0; i < ballsList.Count; i++)
            {
                //Console.WriteLine("PLAYER {0} has ID {1}",i, AllPlayers[i]);
                var player = client.GetGrain<IPlayer>(AllPlayers[i]);
                //player.Initialize
            }
        }
    }
}