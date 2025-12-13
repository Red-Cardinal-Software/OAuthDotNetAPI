using Application.Interfaces.Validation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Infrastructure.Web.Validation;

public class ProblemDetailsValidationResponseFactory : IValidationResponseFactory
{
    public IActionResult Create(ActionContext context, ValidationResult result)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var problemDetails = new ValidationProblemDetails(modelState)
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = "See the errors property for details.",
            Instance = context.HttpContext.Request.Path
        };

        return new UnprocessableEntityObjectResult(problemDetails);
    }
}
