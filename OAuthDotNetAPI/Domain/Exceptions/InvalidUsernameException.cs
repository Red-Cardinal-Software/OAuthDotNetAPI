namespace Domain.Exceptions;

public class InvalidUsernameException(string name) : DomainException($"User's Username '{name}' is invalid.");