using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Infrastructure;
using Quartz;
using MongoDB.Driver;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Core.Services
{
    public class CleanupJob : IJob
    {
        private readonly MongoDbContext _context;
        public CleanupJob(MongoDbContext context)
        {
            _context = context;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
                var thirtyDaysAgo = DateTime.Now.AddDays(-3);

                var question = Builders<Question>.Filter.And(
                            Builders<Question>.Filter.Eq(a => a.IsDeleted, true),
                            Builders<Question>.Filter.Lte(a => a.UpdatedAt, thirtyDaysAgo));

                var room = Builders<Room>.Filter.And(
                            Builders<Room>.Filter.Or(
                                Builders<Room>.Filter.Eq(a => a.IsDeleted, true),
                                Builders<Room>.Filter.Eq(a => a.IsActive, false),
                                Builders<Room>.Filter.Eq(a => a.Status, "ended")
                            ),
                            Builders<Room>.Filter.Lte(a => a.UpdatedAt, thirtyDaysAgo));

                var deleteQuestion =await _context.Questions.DeleteManyAsync(question);
                var deleteRoom =await _context.Rooms.DeleteManyAsync(room);
                
                Console.WriteLine($"Cleaned up {deleteQuestion.DeletedCount} questions at {DateTime.UtcNow}");
                Console.WriteLine($"Cleaned up {deleteRoom.DeletedCount} rooms at {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up wishlists: {ex.Message}");
            }
            
        }
    }
}
