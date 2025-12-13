namespace Domain.Exceptions;

public class InvalidStateTransitionException(string message) : DomainException(message);
