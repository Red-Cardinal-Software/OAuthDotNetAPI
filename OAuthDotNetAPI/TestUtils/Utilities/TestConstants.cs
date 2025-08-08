namespace TestUtils.Utilities;

public class TestConstants
{
    public static class Ids
    {
        public static readonly Guid OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    public static class Emails
    {
        public const string Default = "user@example.com";
    }

    public static class Passwords
    {
        public const string Default = "password";
    }

    public static class Names
    {
        public const string DefaultFirstName = "test";
        public const string DefaultLastName = "user";
    }

    public static class Roles
    {
        public const string DefaultName = "DefaultRole";
    }
}
