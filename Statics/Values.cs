using System;
using System.Collections.Generic;
using System.Text;

namespace Statics
{
    public class Values
    {
        public const string ClusterCollection = "ClusterprojectCollection";
        public const string ConnectionString = "host=localhost;database=OrleansStorage;password=postgres;username=postgres";
        public const string SQLInvariant = "Npgsql";

        public static readonly Guid[] Players = new Guid[10]
        {
            Guid.Parse("b6aa8d78-b052-42ec-b19e-519a1180fb40"),
            Guid.Parse("9e89734e-0aaa-4ddf-b14e-fe0aab993a3c"),
            Guid.Parse("96508dc0-c5ac-4d7b-8428-8492c98405bd"),
            Guid.Parse("41353172-8a62-460a-8b9c-5d9c110063a2"),
            Guid.Parse("12838976-9942-4f3c-96e4-460a025dc1bc"),
            Guid.Parse("635c8c87-df0b-4521-8255-65335a7c9a4e"),
            Guid.Parse("123f2b8a-8437-4890-b77a-8b1bd368e729"),
            Guid.Parse("3affbaaf-4e9b-4b62-adfd-b7ee43676948"),
            Guid.Parse("7913295c-f109-4e34-a5a4-5d6d7221c4af"),
            Guid.Parse("8b1dae0b-5d84-4c5b-9d03-827cffc1b1c8")
        };

        public static readonly Guid[] Balls = new Guid[9]
        {
            Guid.Parse("e3906cb6-e252-4963-9880-b3a00fea7413"),
            Guid.Parse("9d1a3df8-efd8-4605-a0cd-dfb9200de5c0"),
            Guid.Parse("1b4cce52-c2be-4e87-806e-c7d60a1036fb"),
            Guid.Parse("2bc6370b-4c40-4f15-973d-8ba1f795f531"),
            Guid.Parse("711bd588-cd06-423a-a8f3-90d4eb4c5594"),
            Guid.Parse("764a6ddd-ba3e-4357-99f7-0c1d02b44705"),
            Guid.Parse("5abe8697-0e4d-4c5d-8538-94f70cc6c93d"),
            Guid.Parse("881b1ba7-cc7b-4123-ba76-b158557e8693"),
            Guid.Parse("1f1a6c8c-1fd9-436e-b65b-6c81fab1dc3e")
        };

        public static int N { get { return Players.Length; } }
        public static int Kmax { get { return Balls.Length; } }

        //Seconds delay, minimum value for Reminders are 1 minute.
        public const int MaxWaitReminderTime = 1800;
        public const int WaitTimeMin = 60;
        public const int WaitTimeMax = 120;
        public const string TossReminderName = "TossReminder";
        public const string PassReminderName = "PassReminder";

        //Random variables
        public const int MinChange = 1; // Just for semantics
        public const int MaxChance = 100;
        public const int TossChange = 50; //%
        public static readonly Random Randomizer = new Random();
    }
}
