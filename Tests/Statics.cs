using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    class Statics
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
    }
}
