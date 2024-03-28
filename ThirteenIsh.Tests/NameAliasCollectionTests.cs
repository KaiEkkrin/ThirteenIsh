using Shouldly;
using System.Text;
using ThirteenIsh.Game;
using Xunit.Abstractions;

namespace ThirteenIsh.Tests;

// TODO Fix it so that:
// - (Harder) Every unique name always maps to the same alias prefix where possible, and every time we see
// a new name, it is mapped to a different alias prefix if possible. (E.g. "Kobold Archer", "Kobold Alchemist",
// "Kobold Archer", "Kobold Alchemist" with alias length 4 should map to e.g. KobA1, KoAl1, KobA2, KoAl2.)
// Means I need to initialise the NameAliasCollection with the name for each alias, and reconstruct the mapping
// as best I can on construction (using mappings with least ambiguity where no unambiguous mapping exists.)
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
        SortedDictionary<string, string> namesByAlias = [];
        foreach (var name in names)
        {
            NameAliasCollection collection = new(namesByAlias.Keys);
            var alias = collection.Add(name, prefixLength, alwaysAddNumber);
            namesByAlias.ShouldNotContainKey(alias, name);
            namesByAlias.Add(alias, name);
        }

        foreach (var alias in namesByAlias.Keys)
        {
            testOutputHelper.WriteLine(alias);
        }

        // TODO check output looks sane
        // existingAliases.ShouldBeEmpty();
        namesByAlias.Count.ShouldBe(names.Length);
        foreach (var (alias, name) in namesByAlias)
        {
            AssertContainsFirstCharacterOfEachNamePart(alias, name, prefixLength);
        }
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
        SortedDictionary<string, string> namesByAlias = [];
        foreach (var name in names)
        {
            var alias = collection.Add(name, prefixLength, alwaysAddNumber);
            namesByAlias.ShouldNotContainKey(alias, name);
            namesByAlias.Add(alias, name);
        }

        foreach (var alias in namesByAlias.Keys)
        {
            testOutputHelper.WriteLine(alias);
        }

        // TODO check output looks sane
        // collection.Aliases.ShouldBeEmpty();
        namesByAlias.Count.ShouldBe(names.Length);
        collection.Aliases.Order().ShouldBe(namesByAlias.Keys);
        foreach (var (alias, name) in namesByAlias)
        {
            AssertContainsFirstCharacterOfEachNamePart(alias, name, prefixLength);
        }
    }

    private void AssertContainsFirstCharacterOfEachNamePart(string alias, string name, int prefixLength)
    {
        // Build a regex pattern this alias should match:
        StringBuilder patternBuilder = new("^");

        AttributeName.TryCanonicalizeMultiPart(name, out var canonicalizedName).ShouldBeTrue(name);
        var nameParts = canonicalizedName.Split(' ');
        if (nameParts.Length > prefixLength) nameParts = nameParts[..prefixLength];

        foreach (var namePart in nameParts)
        {
            patternBuilder.Append(namePart[0]);
            patternBuilder.Append(@"\p{Ll}*"); // lowercase characters
        }

        patternBuilder.Append(@"[0-9]*$"); // number and end-of-line
        var pattern = patternBuilder.ToString();
        alias.ShouldMatch(pattern, $"First characters of: {string.Join(", ", nameParts)}");
    }
}
