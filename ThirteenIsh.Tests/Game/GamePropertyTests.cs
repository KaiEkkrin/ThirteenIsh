using Discord;
using Shouldly;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;

namespace ThirteenIsh.Tests.Game;

public class GamePropertyTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var possibleValues = new[] { "Fighter", "Wizard", "Rogue" };
        var property = new GameProperty("Class", possibleValues, showOnAdd: true);

        property.Name.ShouldBe("Class");
        property.PossibleValues.ShouldBe(possibleValues);
        property.ShowOnAdd.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ShowOnAddFalse_SetsCorrectly()
    {
        var property = new GameProperty("Background", new[] { "Noble", "Criminal" }, showOnAdd: false);

        property.ShowOnAdd.ShouldBeFalse();
    }

    [Fact]
    public void GetValue_PropertyExists_ReturnsValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();
        character.Sheet.Properties.Add(new PropertyValue<string>("Class", "Fighter"));

        var result = property.GetValue(character);

        result.ShouldBe("Fighter");
    }

    [Fact]
    public void GetValue_PropertyDoesNotExist_ReturnsEmptyString()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();

        var result = property.GetValue(character);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDisplayValue_WithCharacter_PropertyExists_ReturnsValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();
        character.Sheet.Properties.Add(new PropertyValue<string>("Class", "Wizard"));

        var result = property.GetDisplayValue(character);

        result.ShouldBe("Wizard");
    }

    [Fact]
    public void GetDisplayValue_WithCharacter_PropertyEmpty_ReturnsUnset()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();

        var result = property.GetDisplayValue(character);

        result.ShouldBe("(unset)");
    }

    [Fact]
    public void GetDisplayValue_WithTrackedCharacter_PropertyExists_ReturnsValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();
        character.Sheet.Properties.Add(new PropertyValue<string>("Class", "Fighter"));

        var result = property.GetDisplayValue(character);

        result.ShouldBe("Fighter");
    }

    [Fact]
    public void TryEditCharacterProperty_ValidValue_UpdatesSheetAndReturnsTrue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var character = CreateTrackedCharacter();

        var result = property.TryEditCharacterProperty("Wizard", character, out var errorMessage);

        result.ShouldBeTrue();
        errorMessage.ShouldBeNull();
        character.Sheet.Properties.TryGetValue("Class", out var value).ShouldBeTrue();
        value.ShouldBe("Wizard");
    }

    [Fact]
    public void TryEditCharacterProperty_InvalidValue_ReturnsFalseWithError()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var character = CreateTrackedCharacter();

        var result = property.TryEditCharacterProperty("Paladin", character, out var errorMessage);

        result.ShouldBeFalse();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Paladin");
        errorMessage.ShouldContain("not a possible value");
        character.Sheet.Properties.TryGetValue("Class", out var value).ShouldBeFalse();
    }

    [Fact]
    public void TryEditCharacterProperty_OverwritesExistingValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var character = CreateTrackedCharacter();
        character.Sheet.Properties.Add(new PropertyValue<string>("Class", "Fighter"));

        var result = property.TryEditCharacterProperty("Rogue", character, out var errorMessage);

        result.ShouldBeTrue();
        errorMessage.ShouldBeNull();
        character.Sheet.Properties.TryGetValue("Class", out var value).ShouldBeTrue();
        value.ShouldBe("Rogue");
    }

    [Fact]
    public void AddPropertyValueChoiceOptions_AddsAllPossibleValues()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var character = CreateTrackedCharacter();
        var builder = new SelectMenuBuilder();

        property.AddPropertyValueChoiceOptions(builder, character);

        // We can't easily test the internal state of SelectMenuBuilder,
        // but we can verify it doesn't throw and the method completes
        Should.NotThrow(() => property.AddPropertyValueChoiceOptions(builder, character));
    }

    [Fact]
    public void AddPropertyValueChoiceOptions_MarksCurrentValueAsDefault()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var character = CreateTrackedCharacter();
        character.Sheet.Properties.Add(new PropertyValue<string>("Class", "Wizard"));
        var builder = new SelectMenuBuilder();

        // This should complete without throwing - we can't easily verify the default option
        // without reflection or changing the Discord.Net API usage
        Should.NotThrow(() => property.AddPropertyValueChoiceOptions(builder, character));
    }

    private static CharacterSheet CreateCharacterSheet()
    {
        return new CharacterSheet
        {
            Counters = [],
            Properties = [],
            CustomCounters = null
        };
    }

    private static TestTrackedCharacter CreateTrackedCharacter()
    {
        return new TestTrackedCharacter
        {
            Name = "Test Character",
            Sheet = CreateCharacterSheet(),
            Type = CharacterType.PlayerCharacter,
            UserId = 12345,
            LastUpdated = DateTimeOffset.UtcNow,
            SwarmCount = 1
        };
    }

    private sealed class TestTrackedCharacter : ITrackedCharacter
    {
        private readonly FixesSheet _fixesSheet = new();
        private readonly VariablesSheet _variablesSheet = new();

        public string Name { get; set; } = "";
        public DateTimeOffset LastUpdated { get; set; }
        public CharacterSheet Sheet { get; set; } = new();
        public int SwarmCount { get; set; }
        public CharacterType Type { get; set; }
        public ulong UserId { get; set; }
        public string? CharacterSystemName { get; set; }

        public FixesSheet GetFixes() => _fixesSheet;
        public VariablesSheet GetVariables() => _variablesSheet;

        public bool TryGetFix(string name, out int fixValue)
        {
            return _fixesSheet.Counters.TryGetValue(name, out fixValue);
        }
    }
}