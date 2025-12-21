using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace AuthService.Presentation;

public static class ResponseExtensions
{
    public static ActionResult ToResponse(this ErrorList errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.IsEmpty)
        {
            return new ObjectResult(Envelope.Error(errors))
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }

        List<ErrorType> distinctErrorTypes = errors
            .Select(x => x.Type)
            .Distinct()
            .ToList();

        int statusCode = distinctErrorTypes.Count > 1
            ? StatusCodes.Status500InternalServerError
            : GetStatusCodeForErrorType(distinctErrorTypes.First());

        Envelope envelope = Envelope.Error(errors);

        return new ObjectResult(envelope) { StatusCode = statusCode };
    }

    private static int GetStatusCodeForErrorType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            ErrorType.FAILURE => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
}