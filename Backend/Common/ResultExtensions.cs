using Backend.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : MapFail(result.Error, controller);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : MapFail(result.Error, controller);

    private static IActionResult MapFail(Error error, ControllerBase controller)
    {
        var status = error.Type switch
        {
            ErrorType.Validation      => 422,
            ErrorType.Unauthorized    => 401,
            ErrorType.Forbidden       => 403,
            ErrorType.NotFound        => 404,
            ErrorType.Conflict        => 409,
            ErrorType.Locked          => 423,
            ErrorType.TooManyRequests => 429,
            _                         => 500
        };
        return controller.StatusCode(status, new { code = error.Code });
    }
}
