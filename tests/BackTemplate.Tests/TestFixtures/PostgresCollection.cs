using Xunit;

namespace BackTemplate.Tests.TestFixtures;

[CollectionDefinition(nameof(PostgresCollection))]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
