using Backend.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Common.Validation;

public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (!ctx.ModelState.IsValid)
        {
            ctx.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                new { code = ErrorCodes.BadRequest })
            {
                StatusCode = 400
            };
            return;
        }

        foreach (var arg in ctx.ActionArguments.Values)
        {
            if (arg is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            var validator = (IValidator?)serviceProvider.GetService(validatorType);
            if (validator is null) continue;

            var validationContext = new ValidationContext<object>(arg);
            var result = await validator.ValidateAsync(
                validationContext,
                ctx.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                ctx.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                    new { code = ErrorCodes.Validation })
                {
                    StatusCode = 422
                };
                return;
            }
        }

        await next();
    }
}
