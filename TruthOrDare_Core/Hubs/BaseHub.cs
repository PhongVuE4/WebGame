using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Common.Exceptions;
using TruthOrDare_Common.Exceptions.Question;

namespace TruthOrDare_Core.Hubs
{
    public class BaseHub : Hub
    {
        protected async Task ExecuteWithErrorHandling(Func<Task> action, bool sendToCaller = true)
        {
            try
            {
                await action();
            }
            catch (RoomAlreadyExistsException ex) // errorCode: 1001
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1001,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNotExistException ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1003,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomPasswordRequired ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1004,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomPasswordIsWrong ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1005,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomRequiredHost ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1006,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomModeException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1007,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (GameMustbePlaying ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1008,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNotFoundPlayerIdException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1009,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (QuestionTypeWrong ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1010,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomEndStatusException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1011,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomResetStatusException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1012,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomAgeGroupException ex) // errorCode: 1013
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1013,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNameRequiredException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1014,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomStartStatusException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1015,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNotYourTurn ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1016,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNextPlayerException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1017,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNoTimestampException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1018,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomNeedMoreTimeException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1019,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (NoActivePlayersException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1020,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerNotActiveException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1021,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (FullPlayerException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1022,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (RoomHaveBeenStarted ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1023,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerIdNotFound ex) // errorCode: 2003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2003,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerNameExisted ex) // errorCode: 2004
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2004,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerNameRequiredException ex) // errorCode: 2004
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2005,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerIdCannotNull ex) // errorCode: 2006
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2006,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (PlayerIdAlreadyInUseException ex) // errorCode: 2007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2007,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (ValidationException ex) // errorCode: 9998
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 9998,
                            message = ex.Message
                        }
                    });
                }
            }
            catch (Exception ex) // errorCode: 1000 (Unspecified error)
            {
                Console.WriteLine($"Lỗi khi thực thi: {ex.Message}");
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 500,
                        errors = new
                        {
                            errorCode = 1000,
                            message = $"Lỗi không xác định: {ex.Message}"
                        }
                    });
                }
            }
        }
        protected async Task<T> ExecuteWithErrorHandling<T>(Func<Task<T>> action, bool sendToCaller = true)
        {
            try
            {
                return await action();
            }
            catch (RoomAlreadyExistsException ex) // errorCode: 1001
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1001,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNotExistException ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1003,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomRequiredHost ex) // errorCode: 1003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1006,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomModeException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1007,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (GameMustbePlaying ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1008,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNotFoundPlayerIdException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1009,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (QuestionTypeWrong ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1010,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomEndStatusException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1011,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomResetStatusException ex) // errorCode: 1007
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1012,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomAgeGroupException ex) // errorCode: 1013
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1013,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNameRequiredException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1014,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomStartStatusException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1015,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNotYourTurn ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1016,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNextPlayerException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1017,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNoTimestampException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1018,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomNeedMoreTimeException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1019,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (NoActivePlayersException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1020,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (PlayerNotActiveException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1021,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (FullPlayerException ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1022,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (RoomHaveBeenStarted ex) // errorCode: 1014
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 1023,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (PlayerIdNotFound ex) // errorCode: 2003
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2003,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (PlayerNameExisted ex) // errorCode: 2004
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2004,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (PlayerNameRequiredException ex) // errorCode: 2004
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2005,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (PlayerIdCannotNull ex) // errorCode: 2006
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 2006,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (ValidationException ex) // errorCode: 9998
            {
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new
                        {
                            errorCode = 9998,
                            message = ex.Message
                        }
                    });
                }
                throw;
            }
            catch (Exception ex) // errorCode: 1000 (Unspecified error)
            {
                Console.WriteLine($"Lỗi khi thực thi: {ex.Message}");
                if (sendToCaller)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 500,
                        errors = new
                        {
                            errorCode = 1000,
                            message = $"Lỗi không xác định: {ex.Message}"
                        }
                    });
                }
                throw;
            }
        }
    }
}
