using Microsoft.AspNetCore.Mvc;

namespace Application.Interfaces.Validation;

/// <summary>
/// Defines a factory for creating validation response objects.
/// </summary>
public interface IValidationResponseFactory
{
    /// <summary>
    /// Creates a validation response based on the provided action context and validation result.
    /// </summary>
    /// <param name="context">The action context containing HTTP context and route data information.</param>
    /// <param name="result">The validation result containing details of validation errors.</param>
    /// <returns>An <see cref="IActionResult"/> representing the validation response.</returns>
    IActionResult Create(ActionContext context, FluentValidation.Results.ValidationResult result);
}