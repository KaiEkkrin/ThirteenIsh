using Shouldly;
using ThirteenIsh.Game;
using Xunit.Abstractions;

namespace ThirteenIsh.Tests;

// TODO Fix it so that:
// - (Easy) Every name part always gets at least 1 character represented in the alias (up to the alias length).
// This will fix the most trivial cases of ambiguity.
// - (Harder) Every unique name always maps to the same alias prefix where possible, and every time we see
// a new name, it is mapped to a different alias prefix if possible. (E.g. "Kobold Archer", "Kobold Alchemist",
// "Kobold Archer", "Kobold Alchemist" with alias length 4 should map to e.g. KobA1, KoAl1, KobA2, KoAl2.)
// Means I need to initialise the NameAliasCollection with the name for each alias, and reconstruct the mapping
// as best I can on construction (using mappings with least ambiguity where no unambiguous mapping exists.)
// I think it might be reasonable to consider this functionality non-essential and defer it until after I've
// done the non-MVP stuff.
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
