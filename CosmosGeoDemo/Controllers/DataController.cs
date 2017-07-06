
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace CosmosGeoDemo.Controllers
{
    public class DataController : ApiController
    {
        private static readonly Guid[] DeviceIds =
{
            Guid.Parse("cef8c1a8-4a58-42d9-bc90-54722487fc14"),
            Guid.Parse("9a95001a-a9c0-402d-a867-af00096f649f"),
            Guid.Parse("0885accd-4148-43d8-8275-e2ae69edb6dc"),
            Guid.Parse("3f75b612-76f1-452a-9f8b-15e6e429ad52"),
            Guid.Parse("89d92572-9cc3-4fb7-afc3-00f8edd01517"),
            Guid.Parse("ad3928af-e04d-4044-a9fe-4dafb29a0257"),
            Guid.Parse("51a16e78-6832-4d2a-aa8a-8f35e379b562"),
            Guid.Parse("1d07dc22-43cb-48ce-83aa-3626a8bcea6f"),
            Guid.Parse("d90b5bc3-bc27-4e39-bd62-f606e3f25a82"),
            Guid.Parse("37421b0c-bce2-4096-b0e4-a0b574d89b78")
        };

        private static readonly Random Randomizer = new Random(Environment.TickCount);

        public object Get(string id)
        {
            var parts = id.Split('-');

            var key = parts[0];
            var geo = parts[1] + parts[2];

            var endpoint = ConfigurationManager.AppSettings["cosmos-endpoint-" + key];
            var authkey = ConfigurationManager.AppSettings["cosmos-authkey-" + key];
            var database = ConfigurationManager.AppSettings["database"];
            var collection = ConfigurationManager.AppSettings["collection"];

            var collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);

            var policy = new ConnectionPolicy();

            switch (geo)
            {
                case "westus":
                    policy.PreferredLocations.Add(LocationNames.WestUS);
                    break;

                case "eastasia":
                    policy.PreferredLocations.Add(LocationNames.EastAsia);
                    break;

                case "northeurope":
                    policy.PreferredLocations.Add(LocationNames.NorthEurope);
                    break;
            }

            using (var client = new DocumentClient(new Uri(endpoint), authkey, policy))
            {
                var sql = "SELECT VALUE SUM(c.count) FROM c WHERE c.ismeta = true";

                var feedOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    MaxDegreeOfParallelism = 10
                };

                var query = client.CreateDocumentQuery(collectionUri, sql, feedOptions);

                var count = query.ToArray().Single();

                return new { count = count };
            }
        }

        public Task Post()
        {
            return Task.WhenAll(Post("red"), Post("green"), Post("blue"));
        }

        private async Task Post(string key)
        {
            var endpoint = ConfigurationManager.AppSettings["cosmos-endpoint-" + key];
            var authkey = ConfigurationManager.AppSettings["cosmos-authkey-" + key];
            var database = ConfigurationManager.AppSettings["database"];
            var collection = ConfigurationManager.AppSettings["collection"];

            var collectionUri = UriFactory.CreateDocumentCollectionUri(database, collection);

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            Func<string> getRandom = () =>
            {
                var length = 512 * Randomizer.Next(1, 6);
                return new string(Enumerable.Repeat(chars, length).Select(s => s[Randomizer.Next(s.Length)]).ToArray());
            };

            var deviceId = DeviceIds[Randomizer.Next(0, DeviceIds.Length)];

            using (var client = new DocumentClient(new Uri(endpoint), authkey))
            {
                var options = new RequestOptions
                {
                    PostTriggerInclude = new List<string> { "trackDocumentCount" }
                };

                for (var i = 0; i < Randomizer.Next(50, 201); i++)
                {
                    await client.CreateDocumentAsync(collectionUri, new
                    {
                        id = Guid.NewGuid(),
                        deviceId = deviceId,
                        date = DateTimeOffset.UtcNow,
                        currentRpms = Randomizer.NextDouble() * 1000,
                        currentTemp = Randomizer.Next(20, 50),
                        state = getRandom()
                    }, options);
                }
            }
        }
    }
}
