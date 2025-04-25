using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions;
using TruthOrDare_Common;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Question;
using TruthOrDare_Common.Exceptions.GameSession;

namespace TruthOrDare_Common.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (RoomAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomAlreadyExists, ex.Message);
            }
            catch (FullPlayerException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.FullPlayer, ex.Message);
            }
            catch (RoomNotExistException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomIdNotFound, ex.Message);
            }
            catch (RoomPasswordRequired ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomPasswordRequired, ex.Message);
            }
            catch (RoomPasswordWrong ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomPasswordIsWrong, ex.Message);
            }
            catch (InvalidPlayerNameException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerNameLength, ex.Message);
            }
            catch (PlayerNameExisted ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerNameExisted, ex.Message);
            }
            catch (PlayerIdNotFound ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerIdNotFound, ex.Message);
            }
            catch (QuestionFieldsRequiredException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.QuestionTextRequired, ex.Message);
            }
            catch (InvalidQuestionModeException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidQuestionMode, ex.Message);
            }
            catch (InvalidQuestionTypeException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidQuestionType, ex.Message);
            }
            catch (InvalidQuestionDifficultyException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidQuestionDifficulty, ex.Message);
            }
            catch (InvalidQuestionAgeGroupException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidQuestionAgeGroup, ex.Message);
            }
            catch (InvalidTimeLimitException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidTimeLimit, ex.Message);
            }
            catch (InvalidPointsException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidPoints, ex.Message);
            }
            catch (QuestionAlreadyExistsException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.QuestionAlreadyExists, ex.Message);
            }
            catch (QuestionNotFoundException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.QuestionNotFound, ex.Message);
            }
            catch (EmptyQuestionListException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.EmptyQuestionList, ex.Message);
            }
            catch (InvalidFiltersException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.InvalidFilters, ex.Message);
            }
            catch (MultipleValidationException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.MultipleValidationErrors, ex.Message, ex.Errors.ToDictionary(e => e, e => new string[] { e }));
            }
            catch (RoomRequiredHost ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomRequiredHost, ex.Message);
            }
            catch (RoomModeException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomModeException, ex.Message);
            }
            catch (PlayerIdCannotNull ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerIdCannotNull, ex.Message);
            }
            catch (GameMustbePlaying ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.GameMustbePlaying, ex.Message);
            }
            catch (RoomNotFoundPlayerIdException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNotFoundPlayerIdException, ex.Message);
            }
            catch (QuestionTypeWrong ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.QuestionTypeWrong, ex.Message);
            }
            catch (RoomEndStatusException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomEndStatusException, ex.Message);
            }
            catch (RoomResetStatusException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomResetStatusException, ex.Message);
            }
            catch (RoomAgeGroupException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomAgeGroupException, ex.Message);
            }
            catch (RoomNameRequiredException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNameRequiredException, ex.Message);
            }
            catch (PlayerNameRequiredException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerNameRequiredException, ex.Message);
            }
            catch (RoomStartStatusException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomStartStatusException, ex.Message);
            }
            catch (RoomNotYourTurn ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNotYourTurn, ex.Message);
            }
            catch (RoomNoTimestampException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNoTimestampException, ex.Message);
            }
            catch (RoomNeedMoreTimeException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNeedMoreTimeException, ex.Message);
            }
            catch (RoomNextPlayerException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.RoomNextPlayerException, ex.Message);
            }
            catch (GameSessionRequired ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.GameSessionRequired, ex.Message);
            }
            catch (PlayerIdAlreadyInUseException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerIdAlreadyInUseException, ex.Message);
            }
            catch (NoActivePlayersException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.NoActivePlayersException, ex.Message);
            }
            catch (PlayerNotActiveException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.PlayerNotActiveException, ex.Message);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, (int)ErrorCode.ValidationError, ex.Message);
            }
            catch (ValidationException ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.UnprocessableEntity, ex.ErrorCode, ex.Message, ex.Errors);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, (int)HttpStatusCode.InternalServerError, (int)ErrorCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, int statusCode, int errorCode, string message, IDictionary<string, string[]> errors = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse(statusCode, errorCode, message);
            var result = JsonSerializer.Serialize(errorResponse);
            return context.Response.WriteAsync(result);
        }
    }
}
