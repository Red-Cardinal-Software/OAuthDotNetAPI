using Application.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Starbase.Controllers;

/// <summary>
/// Handles unhandled exceptions and returns consistent JSON error responses.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult HandleError()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();

        // In production, don't expose exception details
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopmentOrTesting = environment is "Development" or "Testing";

        var message = isDevelopmentOrTesting && exceptionFeature?.Error != null
            ? exceptionFeature.Error.Message
            : "An unexpected error occurred. Please try again later.";

        return StatusCode(500, new ServiceResponse<object>
        {
            Success = false,
            Message = message,
            Status = 500
        });
    }
}