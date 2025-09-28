using System.Diagnostics.CodeAnalysis;

namespace ThirteenIsh.Tests.Database;

[CollectionDefinition("Database")]
[SuppressMessage("Design", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "This is a collection definition, not a collection type")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}