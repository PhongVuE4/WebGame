using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Common.Exceptions;

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
    }
}
