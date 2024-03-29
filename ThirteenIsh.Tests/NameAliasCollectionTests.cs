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
    public static TheoryData<int, bool, int, string[]> NameAliasData
    {
        get
        {
            TheoryData<int, bool, int, string[]> data = [];

            AddData(7, false, 1, "Bard", "Ba rd", "Cleric", "Warlock");
            AddData(4, true, 1, "Kobold Archer", "Kobold Warrior", "Kobold Hero");
            AddData(4, true, 1, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
                "Kobold Warrior");

            AddData(4, true, 2, "Kobold Archer", "Kobold Archer", "Kobold Warrior", "Kobold Archer", "Kobold Hero",
                "Kobold War Hero", "Kobold Alchemist", "Kobold Archer");

            // This one necessarily has lots of ambiguity because the identical name part prefixes
            // are longer than the max prefix length for the aliases
            AddData(4, true, 8, "Kobold Archer", "Kobold Archer", "Kobold Archmage", "Koboldkin Archer", "Komodo Archer",
                "Komodo Arch Wizard", "Komodo Archmage", "Kobold Archmage", "Kobold Arch Wizard", "Koboldspawn Archer");

            // Some more ambiguity
            AddData(4, true, 3,
                "Old Golem",
                "Oiled Golem",
                "Old Gargoyle",
                "Oiled Gargoyle",
                "Olfactory Golem",
                "Olfactory Gorgon",
                "Olfactory Gargoyle");

            // "Ol G" can't help but be ambiguous
            AddData(4, true, 6,
                "Old Golem",
                "Oiled Golem",
                "Old Gargoyle",
                "Oiled Gargoyle",
                "Olfactory Golem",
                "Olfactory Gorgon",
                "Olfactory Gargoyle",
                "Ol G");

            // TODO can I use a heuristic (try to spread the characters evenly between name parts rather
            // than preferring to grab many characters from the first one...?) to make this pass?
            // Nearly there...
            AddData(5, true, 1,
                "Old Golem",
                "Oiled Golem",
                "Old Gargoyle",
                "Oiled Gargoyle",
                "Olfactory Golem",
                "Olfactory Gorgon",
                "Olfactory Gargoyle");

            AddDataRepeating(3, true, 10, 1, "Aaaaaa", "Aaaaab", "Aaaaac", "Aaaaaa", "Aaaaab", "Aaaaad");
            AddDataRepeating(3, true, 10, 1, "A B C D E", "A B C D F", "A B C D G", "A B D E F", "A B D E G");

            return data;
            void AddData(int prefixLength, bool alwaysAddNumber, int maxAmbiguity, params string[] names)
            {
                data.Add(prefixLength, alwaysAddNumber, maxAmbiguity, names);
            }

            void AddDataRepeating(int prefixLength, bool alwaysAddNumber, int maxAmbiguity, int repeatCount,
                params string[] names)
            {
                var repeatingNames = new string[names.Length * repeatCount];
                for (var i = 0; i < repeatCount; ++i)
                {
                    names.CopyTo(repeatingNames, i * names.Length);
                }

                data.Add(prefixLength, alwaysAddNumber, maxAmbiguity, repeatingNames);
            }
        }
    }

    [Theory]
    [MemberData(nameof(NameAliasData))]
    public void UniqueAliasesAreGeneratedBuildingCollectionEachTime(
        int prefixLength, bool alwaysAddNumber, int maxAmbiguity, params string[] names)
    {
        SortedDictionary<string, string> namesByAlias = [];
        NameAliasCollection? collection = null;
        foreach (var name in names)
        {
            collection = new(namesByAlias.Select(pair => (Alias: pair.Key, Name: pair.Value)));
            var alias = collection.Add(name, prefixLength, alwaysAddNumber);
            namesByAlias.ShouldNotContainKey(alias, name);
            namesByAlias.Add(alias, name);
        }

        foreach (var (alias, name) in namesByAlias)
        {
            testOutputHelper.WriteLine($"'{alias,20}' <- '{name}'");
        }

        collection.ShouldNotBeNull(); // sanity check -- should have some input!

        // TODO check output looks sane
        // existingAliases.ShouldBeEmpty();
        namesByAlias.Count.ShouldBe(names.Length);
        foreach (var (alias, name) in namesByAlias)
        {
            AssertIsValidAliasForName(alias, name, prefixLength);
            AssertAmbiguity(alias, name, maxAmbiguity, collection);
        }
    }

    [Theory]
    [MemberData(nameof(NameAliasData))]
    public void UniqueAliasesAreGeneratedWithPersistentCollection(
        int prefixLength, bool alwaysAddNumber, int maxAmbiguity, params string[] names)
    {
        NameAliasCollection collection = new([]);
        SortedDictionary<string, string> namesByAlias = [];
        foreach (var name in names)
        {
            var alias = collection.Add(name, prefixLength, alwaysAddNumber);
            namesByAlias.ShouldNotContainKey(alias, name);
            namesByAlias.Add(alias, name);
        }

        foreach (var (alias, name) in namesByAlias)
        {
            testOutputHelper.WriteLine($"'{alias,20}' <- '{name}'");
        }

        // TODO check output looks sane
        // collection.Aliases.ShouldBeEmpty();
        namesByAlias.Count.ShouldBe(names.Length);
        collection.Aliases.Order().ShouldBe(namesByAlias.Keys);
        foreach (var (alias, name) in namesByAlias)
        {
            AssertIsValidAliasForName(alias, name, prefixLength);
            AssertAmbiguity(alias, name, maxAmbiguity, collection);
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

    private static void AssertAmbiguity(string alias, string name, int maxAmbiguity, NameAliasCollection collection)
    {
        var message = $"'{alias}' alias of '{name}'";

        var ambiguity = collection.CheckAmbiguity(alias);
        ambiguity.ShouldSatisfyAllConditions(
            () => ambiguity.ShouldBeGreaterThanOrEqualTo(1, message), // must be in the collection
            () => ambiguity.ShouldBeLessThanOrEqualTo(maxAmbiguity, message));
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
