namespace WebApi.Integration.Tests.Fixtures;

/// <summary>
/// xUnit collection definition that shares the SQL Server container across all tests.
/// Tests in this collection will share the same database container instance.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "Integration Tests";
}