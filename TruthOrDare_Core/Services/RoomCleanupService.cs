using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.Models;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_Core.Services
{
    public class RoomCleanupService : BackgroundService
    {
        private readonly IMongoCollection<Room> _rooms;
        //private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        public RoomCleanupService(MongoDbContext dbContext)
        {
            _rooms = dbContext.Rooms;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var filter = Builders<Room>.Filter
                        .And(
                            Builders<Room>.Filter.Eq(r => r.PlayerCount, 0),
                            Builders<Room>.Filter.Eq(r => r.IsActive, true),
                            Builders<Room>.Filter.Where(r => !r.Players.Any(p => p.IsActive))
                        );
                    var rooms = await _rooms.Find(filter).ToListAsync();

                    foreach (var room in rooms)
                    {
                        room.IsActive = false;
                        await _rooms.ReplaceOneAsync(r => r.RoomId == room.RoomId, room);
                        Console.WriteLine($"Room {room.RoomId} set to inactive due to no active players.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in RoomCleanupService: {ex.Message}");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
