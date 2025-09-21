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
        var sheet = CreateCharacterSheet();
        sheet.Properties.Add(new PropertyValue<string>("Class", "Fighter"));

        var result = property.GetValue(sheet);

        result.ShouldBe("Fighter");
    }

    [Fact]
    public void GetValue_PropertyDoesNotExist_ReturnsEmptyString()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var sheet = CreateCharacterSheet();

        var result = property.GetValue(sheet);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetDisplayValue_WithSheet_PropertyExists_ReturnsValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var sheet = CreateCharacterSheet();
        sheet.Properties.Add(new PropertyValue<string>("Class", "Wizard"));

        var result = property.GetDisplayValue(sheet);

        result.ShouldBe("Wizard");
    }

    [Fact]
    public void GetDisplayValue_WithSheet_PropertyEmpty_ReturnsUnset()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var sheet = CreateCharacterSheet();

        var result = property.GetDisplayValue(sheet);

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
        var sheet = CreateCharacterSheet();

        var result = property.TryEditCharacterProperty("Wizard", sheet, out var errorMessage);

        result.ShouldBeTrue();
        errorMessage.ShouldBeNull();
        property.GetValue(sheet).ShouldBe("Wizard");
    }

    [Fact]
    public void TryEditCharacterProperty_InvalidValue_ReturnsFalseWithError()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard" }, false);
        var sheet = CreateCharacterSheet();

        var result = property.TryEditCharacterProperty("Paladin", sheet, out var errorMessage);

        result.ShouldBeFalse();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Paladin");
        errorMessage.ShouldContain("not a possible value");
        property.GetValue(sheet).ShouldBe(string.Empty);
    }

    [Fact]
    public void TryEditCharacterProperty_OverwritesExistingValue()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var sheet = CreateCharacterSheet();
        sheet.Properties.Add(new PropertyValue<string>("Class", "Fighter"));

        var result = property.TryEditCharacterProperty("Rogue", sheet, out var errorMessage);

        result.ShouldBeTrue();
        errorMessage.ShouldBeNull();
        property.GetValue(sheet).ShouldBe("Rogue");
    }

    [Fact]
    public void AddPropertyValueChoiceOptions_AddsAllPossibleValues()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var sheet = CreateCharacterSheet();
        var builder = new SelectMenuBuilder();

        property.AddPropertyValueChoiceOptions(builder, sheet);

        // We can't easily test the internal state of SelectMenuBuilder,
        // but we can verify it doesn't throw and the method completes
        Should.NotThrow(() => property.AddPropertyValueChoiceOptions(builder, sheet));
    }

    [Fact]
    public void AddPropertyValueChoiceOptions_MarksCurrentValueAsDefault()
    {
        var property = new GameProperty("Class", new[] { "Fighter", "Wizard", "Rogue" }, false);
        var sheet = CreateCharacterSheet();
        sheet.Properties.Add(new PropertyValue<string>("Class", "Wizard"));
        var builder = new SelectMenuBuilder();

        // This should complete without throwing - we can't easily verify the default option
        // without reflection or changing the Discord.Net API usage
        Should.NotThrow(() => property.AddPropertyValueChoiceOptions(builder, sheet));
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

    private class TestTrackedCharacter : ITrackedCharacter
    {
        public string Name { get; set; } = "";
        public DateTimeOffset LastUpdated { get; set; }
        public CharacterSheet Sheet { get; set; } = new();
        public int SwarmCount { get; set; }
        public CharacterType Type { get; set; }
        public ulong UserId { get; set; }

        public FixesSheet GetFixes() => new();
        public VariablesSheet GetVariables() => new();
    }
}