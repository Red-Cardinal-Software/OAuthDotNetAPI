using Microsoft.AspNetCore.Mvc;

namespace Starbase.Controllers;

/// <summary>
/// Base controller providing common functionality for API controller classes,
/// including utility methods for resolving and handling synchronous and
/// asynchronous operations within the context of an HTTP request.
/// </summary>
public class BaseAppController(ILogger logger) : ControllerBase
{
    /// <summary>
    /// Resolves the execution of a given function and returns an HTTP action result.
    /// This is for synchronous service methods
    /// This method handles any exceptions occurring during the function execution
    /// and ensures a proper HTTP response is returned. If the operation completes
    /// successfully, it returns an HTTP 200 status code with the function result.
    /// If an exception occurs, it logs the error and returns an HTTP 500 status code.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the function.</typeparam>
    /// <param name="function">The function to execute and resolve into an action result.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> that represents the HTTP response.
    /// On success, it contains the result of the function wrapped in a 200 status.
    /// In case of an error, it contains a 500 status with an appropriate error message.
    /// </returns>
    protected IActionResult Resolve<T>(Func<T> function)
    {
        return ResolveActionResult(() => Ok(function()));
    }

    /// <summary>
    /// Asynchronously resolves the execution of a given asynchronous function and returns an HTTP action result.
    /// This method is designed for resolving asynchronous service methods within the context of an HTTP request.
    /// It ensures proper handling of exceptions and returns appropriate HTTP responses based on the outcome of the function execution.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the asynchronous function.</typeparam>
    /// <param name="function">The asynchronous function to execute and resolve into an action result.</param>
    /// <returns>
    /// A <see cref="Task{IActionResult}"/> representing the asynchronous HTTP response.
    /// On success, it contains the result of the function wrapped in a 200 status.
    /// In the event of an error, it returns appropriate status codes, such as a 500 status for internal errors.
    /// </returns>
    protected async Task<IActionResult> ResolveAsync<T>(Func<Task<T>> function)
    {
        return await ResolveActionResultAsync(async () => Ok(await function()));
    }

    /// <summary>
    /// Resolves the execution of an asynchronous function and returns an HTTP action result.
    /// This method is designed for asynchronous service methods and ensures proper handling
    /// of exceptions during the function execution. If the execution is successful, it returns
    /// an HTTP 200 status code with the function result. If an exception occurs, it logs the error
    /// and returns an HTTP 500 status code.
    /// </summary>
    /// <param name="result">
    /// The asynchronous delegate that returns an <see cref="IActionResult"/>, representing the HTTP response.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> that represents the HTTP response.
    /// On success, it contains the result of the function wrapped in a 200 status.
    /// In the event of an error, it returns a 500 status with an appropriate error message.
    /// </returns>
    private async Task<IActionResult> ResolveActionResultAsync(Func<Task<IActionResult>> result)
    {
        try
        {
            return await result();
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
            return StatusCode(500, "An error occurred performing the request");
        }
    }

    /// <summary>
    /// Resolves the execution of a given function that returns an <see cref="IActionResult"/>
    /// and ensures proper handling of exceptions during execution.
    /// This method provides a unified mechanism for wrapping a function's execution
    /// and translating its outcome to an appropriate HTTP response.
    /// </summary>
    /// <param name="result">A function that produces an <see cref="IActionResult"/> when executed.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> that represents the HTTP response. On success, it returns
    /// the result of the function provided. In case of an exception, it logs the error and returns
    /// an HTTP 500 status with an appropriate error message.
    /// </returns>
    private IActionResult ResolveActionResult(Func<IActionResult> result)
    {
        try
        {
            return result();
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, e.Message);
            return StatusCode(500, "An error occurred performing the request");
        }
    }
}
