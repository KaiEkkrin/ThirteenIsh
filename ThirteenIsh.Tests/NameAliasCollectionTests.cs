using Shouldly;
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
    public static TheoryData<int, bool, string[]> NameAliasData
    {
        get
        {
            TheoryData<int, bool, string[]> data = [];

            AddData(7, false, "Bard", "Ba rd", "Cleric", "Warlock");
            AddData(4, true, "Kobold Archer", "Kobold Warrior", "Kobold Hero");
            AddData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
                "Kobold Warrior");

            AddData(4, true, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
                "Kobold War Hero", "Kobold Alchemist", "Kobold Archer");

            AddDataRepeating(3, true, 10, "Aaaaaa", "Aaaaab", "Aaaaac", "Aaaaaa", "Aaaaab", "Aaaaad");
            AddDataRepeating(3, true, 10, "A B C D E", "A B C D F", "A B C D G", "A B D E F", "A B D E G");

            return data;
            void AddData(int prefixLength, bool alwaysAddNumber, params string[] names)
            {
                data.Add(prefixLength, alwaysAddNumber, names);
            }

            void AddDataRepeating(int prefixLength, bool alwaysAddNumber, int repeatCount, params string[] names)
            {
                var repeatingNames = new string[names.Length * repeatCount];
                for (var i = 0; i < repeatCount; ++i)
                {
                    names.CopyTo(repeatingNames, i * names.Length);
                }

                data.Add(prefixLength, alwaysAddNumber, repeatingNames);
            }
        }
    }

    [Theory]
    [MemberData(nameof(NameAliasData))]
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
            AssertIsValidAliasForName(alias, name, prefixLength);
        }
    }

    [Theory]
    [MemberData(nameof(NameAliasData))]
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
            AssertIsValidAliasForName(alias, name, prefixLength);
        }
    }

    [Theory]
    [InlineData("Bard", "Bard")]
    [InlineData("B", "Bard")]
    [InlineData("Bar", "Bard")]
    [InlineData("Bar1", "Bard")]
    [InlineData("KobHeWi1", "Kobold Hedge Wizard")]
    [InlineData("KoboldHedgeWizard", "Kobold Hedge Wizard")]
    [InlineData("KHW1", "Kobold Hedge Wizard")]
    public void CouldBeAliasFor(string alias, string name)
    {
        var couldBeAliasForName = NameAliasCollection.CouldBeAliasFor(alias, name);
        couldBeAliasForName.ShouldBeTrue();
    }

    [Theory]
    [InlineData("C", "Bard")]
    [InlineData("Bardette", "Bard")]
    [InlineData("HobKeWi1", "Kobold Hedge Wizard")]
    [InlineData("KWH1", "Kobold Hedge Wizard")]
    [InlineData("KobHoWi1", "Kobold Hedge Wizard")]
    [InlineData("KoboldHedgeWizards", "Kobold Hedge Wizard")]
    public void CouldNotBeAliasFor(string alias, string name)
    {
        var couldBeAliasForName = NameAliasCollection.CouldBeAliasFor(alias, name);
        couldBeAliasForName.ShouldBeFalse();
    }

    private static void AssertIsValidAliasForName(string alias, string name, int prefixLength)
    {
        var message = $"'{alias}' alias of '{name}'";

        var match = NameAliasCollection.AliasRegex().Match(alias);
        match.Success.ShouldBeTrue(message);

        var textPart = match.Groups[1].Value;
        var totalPartsLength = name.Split(' ').Sum(namePart => namePart.Length);
        textPart.Length.ShouldBe(Math.Min(prefixLength, totalPartsLength), message);

        NameAliasCollection.CouldBeAliasFor(alias, name).ShouldBeTrue(message);
    }
}
