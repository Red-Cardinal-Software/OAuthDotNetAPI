namespace Domain.Exceptions;

public class DuplicateRoleException(string name) : DomainException($"The user already has a role with the name '{name}'.");
