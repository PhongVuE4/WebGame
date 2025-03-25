using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Numerics;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Infrastructure
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        public IMongoCollection<Question> Questions => GetCollection<Question>("questions");
        public IMongoCollection<Player> Players => GetCollection<Player>("players");
        public IMongoCollection<Room> Rooms => GetCollection<Room>("rooms");
        public IMongoCollection<GameSession> GameSessions => GetCollection<GameSession>("game_sessions");

        public MongoDbContext(IConfiguration config)
        {
            try
            {
                var connectionString = config["MongoDb:ConnectionString"];
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException("MongoDB connection string is missing or empty.");
                }

                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(config["MongoDb:DatabaseName"]);

                // Kiểm tra kết nối bằng cách gọi một lệnh đơn giản
                var dbList = client.ListDatabaseNames().ToList();
            }
            catch (MongoAuthenticationException ex)
            {
                throw new Exception("Failed to authenticate with MongoDB. Check your username, password, or connection string (e.g., ensure authSource=admin is included).", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to connect to MongoDB.", ex);
            }
        }

        
    }
}
