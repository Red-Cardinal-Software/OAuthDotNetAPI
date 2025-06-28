using Application.Models;

namespace Application.Common.Factories;

public static class ServiceResponseFactory
{
    public static ServiceResponse<T> Success<T>(T data, string? message = null) =>
        new() { Data = data, Message = message ?? "" };

    public static ServiceResponse<T> Error<T>(string message) =>
        new() { Data = default, Success = false, Message = message };
}