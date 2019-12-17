using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Collections.Generic;

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
                Console.WriteLine("\n\n Press Enter to start process...\n\n");
                Console.ReadLine();

                var host = await StartSilo();

                var client = await ConnectClient();


                
                Console.WriteLine("\n\n How many players do you want in the game? :");

                List<Guid> AllPlayers = SpawnPlayers(client, int.Parse(Console.ReadLine()));




                Console.WriteLine("\n\n How many balls do you want in the game? :");

                await GiveRandomPlayersBalls(client, AllPlayers, int.Parse(Console.ReadLine()));

                



                Console.WriteLine("Everything has been completed succesfully, bitch!");
                Console.ReadLine();

                await host.StopAsync();

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

            // K balls
            for (int i = 0; i < ballsList.Count; i++)
            {
                Console.WriteLine("PLAYER {0} has ID {1}",i, AllPlayers[i]);
                var player = client.GetGrain<IPlayer>(AllPlayers[i]);
                Console.WriteLine("PLAYER {0} gets ball {1}", i, ballsList[i]);

                await player.ReceiveBall(ballsList[i]);

            }


        }
    }
}