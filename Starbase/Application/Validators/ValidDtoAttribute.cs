using System.Reflection;
using Application.DTOs.Validation;
using Application.Interfaces.Validation;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Validators;

/// <summary>
/// The <c>ValidDtoAttribute</c> class is a custom action filter attribute used for validating
/// Data Transfer Objects (DTOs) in ASP.NET Core MVC action methods.
/// It ensures that the model state is valid and facilitates custom validation logic
/// using FluentValidation.
/// </summary>
/// <remarks>
/// If the model state is invalid, this filter sets the result of the action context to a
/// <c>BadRequestObjectResult</c>. Additionally, it validates any action method arguments
/// that implement the <c>IValidatableDto</c> interface using the specified or default validation rules.
/// </remarks>
/// <example>
/// Use this attribute on controller action methods to enforce DTO validation before proceeding
/// to the action logic.
/// </example>
public sealed class ValidDtoAttribute : ActionFilterAttribute
{
    private readonly string? _ruleset;

    private static readonly MethodInfo ValidateWithRuleset = typeof(FluentExtensions).GetMethod("ValidateWithRuleset")!;

    public ValidDtoAttribute(string ruleset) => _ruleset = ruleset;

    public ValidDtoAttribute() { }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
            return;
        }

        var validationFactory = context.HttpContext.RequestServices.GetRequiredService<IValidationResponseFactory>();

        var args = context.ActionArguments.Where(arg => arg.Value is IValidatableDto);

        foreach (var arg in args)
        {
            if (arg.Value is null)
            {
                context.Result = new UnprocessableEntityObjectResult(new List<ValidationFailure>
                {
                    new(arg.Key, $"{arg.Key} must not be null")
                });
                return;
            }
            var paramType = context.ActionDescriptor.Parameters.First(x => x.Name == arg.Key).ParameterType;
            var validatorType = typeof(IValidator<>).MakeGenericType(paramType);
            var validator = context.HttpContext.RequestServices.GetService(validatorType);

            if (validator is null)
            {
                throw new Exception($"No Validator of {paramType} is registered in Program startup");
            }

            ValidationResult validationResult;

            if (string.IsNullOrEmpty(_ruleset))
            {
                validationResult = await (Task<ValidationResult>)validatorType.GetMethod("ValidateAsync")!.Invoke(
                    validator,
                    [arg.Value, CancellationToken.None])!;
            }
            else
            {
                validationResult = await (Task<ValidationResult>)ValidateWithRuleset.MakeGenericMethod(paramType)
                    .Invoke(validator, [validator, arg.Value, _ruleset])!;
            }

            if (!validationResult.IsValid)
            {
                context.Result = validationFactory.Create(context, validationResult);
                return;
            }
        }

        await next();
    }
}
