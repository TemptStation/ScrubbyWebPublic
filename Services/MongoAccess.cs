using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ScrubbyWeb.Services
{
    public class MongoAccess
    {
        public MongoAccess(IConfiguration configuration)
        {
            var mongoConfig = configuration.GetSection("MongoConfig");
            var clientConfig = new MongoClientSettings
            {
                Server = new MongoServerAddress(mongoConfig["ServerString"],
                    mongoConfig.GetSection("ServerPort").Get<int>()),
                Credential = MongoCredential.CreateCredential(mongoConfig["AuthenticationDatabase"],
                    mongoConfig["Username"], mongoConfig["Password"]),
                ApplicationName = "ScrubbyWeb",
                RetryWrites = false
            };
            Client = new MongoClient(clientConfig);
            DB = Client.GetDatabase("tgstation");
        }

        public IMongoClient Client { get; }
        public IMongoDatabase DB { get; }
    }
}