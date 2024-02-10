using Discord;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
internal class GameCounter(string name, string? alias = null, int defaultValue = 0, int minValue = 0,
    int? maxValue = null, bool hasVariable = false, bool isHidden = false) : GamePropertyBase(name, alias, isHidden)
{
    /// <summary>
    /// The default value for this counter.
    /// </summary>
    public int DefaultValue => defaultValue;

    /// <summary>
    /// The minimum value for this counter.
    /// </summary>
    public int MinValue => minValue;

    /// <summary>
    /// The maximum value for this counter, if there is one.
    /// </summary>
    public int? MaxValue => maxValue;

    public bool HasVariable => hasVariable;

    /// <summary>
    /// Adds a component that would edit this counter's value to the component builder.
    /// </summary>
    public ComponentBuilder AddCharacterEditorComponent(ComponentBuilder componentBuilder,
        string customId, CharacterSheet? sheet)
    {
        if (!CanStore) return componentBuilder; // no editing for this one
        var currentValue = sheet != null ? (int?)GetValue(sheet) : null;
        if (maxValue.HasValue && (maxValue - minValue) <= 25)
        {
            // Represent this as a menu. It helps, since Discord doesn't have number
            // input validation
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"{customId}:{Name}")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithPlaceholder($"Select a {Name} value");

            for (var i = minValue; i <= maxValue.Value; ++i)
            {
                menuBuilder.AddOption($"{i}", $"{i}", isDefault: i == currentValue);
            }

            return componentBuilder.WithSelectMenu(menuBuilder);
        }
        else
        {
            // Sadly this requires a modal instead and would be a massive pain in the butt. :(
            // (Also, discord.net doesn't let me add select menus to modals!)
            throw new NotSupportedException(Name);
        }
    }

    public override void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, CharacterSheet sheet)
    {
        if (!maxValue.HasValue || maxValue.Value - minValue > 25)
            throw new NotSupportedException(
                $"Cannot build property value choice options for {Name} (min = {minValue}, max = {maxValue})");

        var currentValue = GetValue(sheet);
        for (var i = minValue; i <= maxValue.Value; ++i)
        {
            builder.AddOption($"{i}", $"{i}", isDefault: i == currentValue);
        }
    }

    public override string GetDisplayValue(CharacterSheet sheet)
    {
        return GetValue(sheet) is { } value ? $"{value}" : Unset;
    }

    /// <summary>
    /// Gets the starting value of this counter's variable from the character sheet
    /// (only relevant if it has an associated variable.)
    /// </summary>
    public virtual int? GetStartingValue(CharacterSheet characterSheet)
    {
        return GetValue(characterSheet);
    }

    /// <summary>
    /// Gets this counter's value from the character sheet.
    /// </summary>
    public virtual int? GetValue(CharacterSheet characterSheet)
    {
        return characterSheet.Counters.TryGetValue(Name, out var value) ? value : defaultValue;
    }

    public override bool TryEditCharacterProperty(string newValue, CharacterSheet sheet,
        [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!int.TryParse(newValue, out var newValueInt))
        {
            errorMessage = $"'{newValue}' is not a possible value for {Name}.";
            return false;
        }

        if (maxValue.HasValue)
        {
            if (newValueInt < minValue || newValueInt > maxValue.Value)
            {
                errorMessage = $"{Name} must be between {minValue} and {maxValue}.";
                return false;
            }
        }
        else
        {
            if (newValueInt < minValue)
            {
                errorMessage = $"{Name} must not be less than {minValue}.";
                return false;
            }
        }

        sheet.Counters[Name] = newValueInt;
        errorMessage = null;
        return true;
    }
}

