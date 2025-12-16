namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when invalid parameters are provided for creating a login attempt.
/// This exception enforces domain invariants for login attempt creation.
/// </summary>
public class InvalidLoginAttemptException(string message) : DomainException(message);