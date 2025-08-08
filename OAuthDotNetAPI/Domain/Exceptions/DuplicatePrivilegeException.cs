namespace Domain.Exceptions;

public class DuplicatePrivilegeException(string name) : DomainException($"The role already has a privilege with the name '{name}'.");
