namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when invalid parameters are provided for account lockout operations.
/// This exception enforces domain invariants for lockout configuration and state transitions.
/// </summary>
public class InvalidLockoutParametersException(string message) : DomainException(message);