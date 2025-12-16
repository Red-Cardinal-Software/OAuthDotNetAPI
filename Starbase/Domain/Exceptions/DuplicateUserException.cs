namespace Domain.Exceptions;

public class DuplicateUserException(string username)
    : DomainException($"A user with the username '{username}' already exists in the organization.");
