using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarePoint.API.Filters;

/// <summary>
/// Runs registered FluentValidation validators for request DTOs before controller actions execute.
/// Keeping this at the HTTP boundary prevents malformed input from reaching application services.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (validationResult.IsValid)
                continue;

            var errors = validationResult.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = "One or more validation errors occurred.",
                errors
            });
            return;
        }

        await next();
    }
}
