using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Core.Services;
using Microsoft.Extensions.Hosting;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Core.Services
{
    public class AutoNextPlayerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private List<Room> _activeRooms = new List<Room>(); // Lưu danh sách phòng trong bộ nhớ
        private DateTime _lastRoomRefresh = DateTime.MinValue;

        public AutoNextPlayerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("AutoNextPlayerService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();

                        // Cập nhật danh sách phòng mỗi 30 giây
                        if ((DateTime.UtcNow - _lastRoomRefresh).TotalSeconds >= 30)
                        {
                            _activeRooms = await roomService.GetActiveRooms();
                            _lastRoomRefresh = DateTime.UtcNow;
                            Console.WriteLine($"Refreshed active rooms: Found {_activeRooms.Count}");
                        }
                        else
                        {
                            Console.WriteLine($"Using cached rooms: Found {_activeRooms.Count}");
                        }

                        foreach (var room in _activeRooms)
                        {
                            double timeElapsed;
                            string timestampType;

                            if (room.LastQuestionTimestamp.HasValue)
                            {
                                timeElapsed = (DateTime.UtcNow - room.LastQuestionTimestamp.Value).TotalSeconds;
                                timestampType = "LastQuestionTimestamp";
                                if (timeElapsed >= 120)
                                {
                                    Console.WriteLine($"Auto-switching player in room {room.RoomId} (after 2 minutes of question)");
                                    var currentPlayerId = room.CurrentPlayerIdTurn;
                                    var (nextPlayerId, isGameEnded, message) = await roomService.NextPlayer(room.RoomId, currentPlayerId);
                                    if (isGameEnded)
                                    {
                                        Console.WriteLine($"Game ended in room {room.RoomId}: {message}");
                                        // Không cần cập nhật thêm vì NextPlayer đã đặt Status = "ended"
                                    }
                                    else
                                    {
                                        room.CurrentPlayerIdTurn = nextPlayerId; // Cập nhật trong bộ nhớ
                                        room.LastQuestionTimestamp = null;
                                        room.LastTurnTimestamp = DateTime.Now;
                                        Console.WriteLine($"Auto-switched from {currentPlayerId} to {nextPlayerId} in room {room.RoomId}");
                                    }
                                }
                            }
                            else if (room.LastTurnTimestamp.HasValue)
                            {
                                timeElapsed = (DateTime.UtcNow - room.LastTurnTimestamp.Value).TotalSeconds;
                                timestampType = "LastTurnTimestamp";
                                if (timeElapsed >= 30)
                                {
                                    Console.WriteLine($"Auto-switching player in room {room.RoomId} (after 30 seconds of turn)");
                                    var currentPlayerId = room.CurrentPlayerIdTurn;
                                    var (nextPlayerId, isGameEnded, message) = await roomService.NextPlayer(room.RoomId, currentPlayerId);

                                    if (isGameEnded)
                                    {
                                        Console.WriteLine($"Game ended in room {room.RoomId}: {message}");
                                    }
                                    else
                                    {
                                        room.CurrentPlayerIdTurn = nextPlayerId;
                                        room.LastQuestionTimestamp = null;
                                        room.LastTurnTimestamp = DateTime.Now;
                                        Console.WriteLine($"Auto-switched from {currentPlayerId} to {nextPlayerId} in room {room.RoomId}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Room {room.RoomId}: No LastQuestionTimestamp or LastTurnTimestamp set.");
                                continue;
                            }

                            Console.WriteLine($"Room {room.RoomId}: {timestampType}={room.LastQuestionTimestamp ?? room.LastTurnTimestamp}, TimeElapsed={timeElapsed} seconds");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AutoNextPlayerService: {ex.Message}");
                }
                await Task.Delay(30000, stoppingToken); // Giữ 30 giây để phản hồi nhanh
            }
        }
    }
}
