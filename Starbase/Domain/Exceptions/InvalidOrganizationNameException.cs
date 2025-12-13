namespace Domain.Exceptions;

public class InvalidOrganizationNameException(string name) : DomainException($"Organization name '{name}' is invalid.");
