using Discord;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game property is an enumerated property of a character sheet, likely to
/// exist in the context of a particular game system. E.g. class or profession.
/// The corresponding entity is a CharacterProperty, stored in a CharacterSheet.
/// </summary>
internal class GameProperty(string name, string[] possibleValues, bool showOnAdd) : GamePropertyBase(name)
{
    public IReadOnlyList<string> PossibleValues => possibleValues;

    public override bool ShowOnAdd => showOnAdd;

    /// <summary>
    /// Adds a component that would edit this property to the component builder.
    /// </summary>
    public ComponentBuilder AddCharacterEditorComponent(ComponentBuilder componentBuilder,
        string customId, CharacterSheet? sheet)
    {
        var currentValue = sheet != null ? GetValue(sheet) : null;
        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId($"{customId}:{Name}")
            .WithMinValues(1)
            .WithMaxValues(1)
            .WithPlaceholder($"Select a {Name}");

        foreach (var possibleValue in possibleValues)
        {
            menuBuilder.AddOption(possibleValue, possibleValue,
                isDefault: possibleValue == currentValue);
        }

        return componentBuilder.WithSelectMenu(menuBuilder);
    }

    public override void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, CharacterSheet sheet)
    {
        var currentValue = GetValue(sheet);
        foreach (var possibleValue in possibleValues)
        {
            builder.AddOption(possibleValue, possibleValue, isDefault: possibleValue == currentValue);
        }
    }

    public override string GetDisplayValue(Adventurer adventurer)
    {
        return GetValue(adventurer.Sheet) is { Length: > 0 } value ? value : Unset;
    }

    public override string GetDisplayValue(CharacterSheet sheet)
    {
        return GetValue(sheet) is { Length: > 0 } value ? value : Unset;
    }

    /// <summary>
    /// Gets this property's value from the character sheet.
    /// </summary>
    public string GetValue(CharacterSheet characterSheet)
    {
        return characterSheet.Properties.TryGetValue(Name, out var value) ? value : string.Empty;
    }

    public override bool TryEditCharacterProperty(string newValue, CharacterSheet sheet,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!possibleValues.Contains(newValue))
        {
            errorMessage = $"'{newValue}' is not a possible value for {Name}.";
            return false;
        }

        sheet.Properties[Name] = newValue;
        errorMessage = null;
        return true;
    }
}

