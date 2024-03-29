using Shouldly;
using ThirteenIsh.Game;
using Xunit.Abstractions;

namespace ThirteenIsh.Tests;

// TODO An improvement to NameAliasCollection, if it's still generating overly ambiguous aliases,
// would be to have it try generating a longer alias if the given-length one is too ambiguous.
public class NameAliasCollectionTests(ITestOutputHelper testOutputHelper)
{
    public static TheoryData<int, bool, int, string[]> NameAliasData
    {
        get
        {
            TheoryData<int, bool, int, string[]> data = [];

            AddData(7, false, 1, "Bard", "Ba rd", "Cleric", "Warlock");
            AddData(7, false, 1, "Bard", "Ba rd", "Cleric", "Warlock", "Cleric", "Warlock");
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

            // This one necessarily has lots of ambiguity because "Ol G" is short enough that it overlaps with
            // most of the other aliases
            AddData(4, true, 6,
                "Old Golem",
                "Oiled Golem",
                "Old Gargoyle",
                "Oiled Gargoyle",
                "Olfactory Golem",
                "Olfactory Gorgon",
                "Olfactory Gargoyle",
                "Ol G");

            AddData(5, true, 2,
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
            testOutputHelper.WriteLine($"'{alias,10}' <- '{name}'");
        }

        collection.ShouldNotBeNull(); // sanity check -- should have some input!

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
            testOutputHelper.WriteLine($"'{alias,10}' <- '{name}'");
        }

        namesByAlias.Count.ShouldBe(names.Length);
        collection.Aliases.Order().ShouldBe(namesByAlias.Keys);
        foreach (var (alias, name) in namesByAlias)
        {
            AssertIsValidAliasForName(alias, name, prefixLength);
            AssertAmbiguity(alias, name, maxAmbiguity, collection);
        }
    }

    [Theory]
    [MemberData(nameof(NameAliasData))]
    public void CollectionCanBeReCreatedWithoutChangeInBehaviour(
        int prefixLength, bool alwaysAddNumber, int _, params string[] names)
    {
        // This test just checks that re-creating the collection between every generate results
        // in the same aliases as retaining the existing collection. The Unique* tests check for
        // correctness of the generated aliases.
        NameAliasCollection? ephemeralCollection = null;
        NameAliasCollection persistentCollection = new([]);
        Dictionary<string, string> namesByAlias = [];
        foreach (var name in names)
        {
            ephemeralCollection = new(namesByAlias.Select(pair => (Alias: pair.Key, Name: pair.Value)));
            var ephemeralAlias = ephemeralCollection.Add(name, prefixLength, alwaysAddNumber);
            var persistentAlias = persistentCollection.Add(name, prefixLength, alwaysAddNumber);

            namesByAlias.ShouldNotContainKey(persistentAlias, name);
            namesByAlias.Add(persistentAlias, name);

            ephemeralAlias.ShouldBe(persistentAlias, name);

            testOutputHelper.WriteLine($"'{persistentAlias,10}' <- '{name}'");
        }

        persistentCollection.Aliases.Order().ShouldBe(namesByAlias.Keys.Order());

        ephemeralCollection.ShouldNotBeNull();
        ephemeralCollection.Aliases.Order().ShouldBe(persistentCollection.Aliases.Order());
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
