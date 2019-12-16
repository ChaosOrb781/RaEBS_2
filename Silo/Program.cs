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
                var host = await StartSilo();


                var client = await ConnectClient();

                Console.WriteLine("\n\n Press Enter to start process...\n\n");
                Console.ReadLine();

                // Create Player instance - n players
                var player = client.GetGrain<IPlayer>(Guid.NewGuid());

                //A player with a new GUID is being initialized where he is not holding a ball
                await player.Initialize(new List<Guid>(), false);

                Console.WriteLine("Give the player a ball...");
                Console.ReadLine();
                //Give Player a ball (new ball) - k balls
                //await player.ReceiveBall(Guid.NewGuid());

                

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
                .ConfigureLogging(logging => logging.AddConsole())
                .AddAdoNetGrainStorage("OrleansStorage", options =>
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
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }
    }
}