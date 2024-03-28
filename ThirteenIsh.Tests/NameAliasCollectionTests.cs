using Shouldly;
using ThirteenIsh.Game;
using Xunit.Abstractions;

namespace ThirteenIsh.Tests;

public class NameAliasCollectionTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData(7, false, "Bard", "Cleric", "Warlock")]
    [InlineData(4, true, "Kobold Archer", "Kobold Warrior", "Kobold Hero")]
    [InlineData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero", "Kobold Warrior")]
    [InlineData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
        "Kobold War Hero", "Kobold Alchemist", "Kobold Archer")]
    public void UniqueAliasesAreGeneratedBuildingCollectionEachTime(
        int prefixLength, bool alwaysAddNumber, params string[] names)
    {
        SortedSet<string> existingAliases = [];
        foreach (var name in names)
        {
            NameAliasCollection collection = new(existingAliases);
            var alias = collection.Add(name, prefixLength, alwaysAddNumber);
            existingAliases.ShouldNotContain(alias);
            existingAliases.Add(alias);
        }

        foreach (var alias in existingAliases)
        {
            testOutputHelper.WriteLine(alias);
        }

        // TODO check output looks sane
        // existingAliases.ShouldBeEmpty();
        existingAliases.Count.ShouldBe(names.Length);
    }

    [Theory]
    [InlineData(7, false, "Bard", "Cleric", "Warlock")]
    [InlineData(4, true, "Kobold Archer", "Kobold Warrior", "Kobold Hero")]
    [InlineData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero", "Kobold Warrior")]
    [InlineData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
        "Kobold War Hero", "Kobold Alchemist", "Kobold Archer")]
    public void UniqueAliasesAreGeneratedWithPersistentCollection(
        int prefixLength, bool alwaysAddNumber, params string[] names)
    {
        NameAliasCollection collection = new([]);
        foreach (var name in names)
        {
            collection.Add(name, prefixLength, alwaysAddNumber);
        }

        foreach (var alias in collection.Aliases)
        {
            testOutputHelper.WriteLine(alias);
        }

        // TODO check output looks sane
        // collection.Aliases.ShouldBeEmpty();
        collection.Aliases.ToHashSet().Count.ShouldBe(names.Length);
    }
}
