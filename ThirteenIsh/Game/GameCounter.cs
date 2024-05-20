using Discord;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
internal class GameCounter(string name, string? alias = null,
    int defaultValue = 0, int minValue = 0, int? maxValue = null, GameCounterOptions options = GameCounterOptions.None)
    : GamePropertyBase(name, alias, options.HasFlag(GameCounterOptions.IsHidden))
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

    /// <summary>
    /// This counter's options.
    /// </summary>
    public GameCounterOptions Options => options;

    /// <summary>
    /// Adds a component that would edit this counter's value to the component builder.
    /// </summary>
    public ComponentBuilder AddCharacterEditorComponent(ComponentBuilder componentBuilder,
        string customId, CounterSheet? sheet)
    {
        if (!CanStore) return componentBuilder; // no editing for this one
        var currentValue = sheet != null ? GetValue(sheet) : null;
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

    public override string GetDisplayValue(ITrackedCharacter character)
    {
        return (GetValue(character.Sheet), GetVariableValue(character)) switch
        {
            ({ } maxValue, { } varValue) => $"{varValue}/{maxValue}",
            ({ } maxValue, null) => $"{maxValue}",
            _ => Unset
        };
    }

    public override string GetDisplayValue(CharacterSheet sheet)
    {
        return GetValue(sheet) is { } value ? $"{value}" : Unset;
    }

    /// <summary>
    /// Gets the maximum value of this counter's variable
    /// (only relevant if it has an associated variable.)
    /// </summary>
    public virtual int? GetMaxVariableValue(ICounterSheet sheet)
    {
        return GetValue(sheet);
    }

    /// <summary>
    /// Gets the starting value of this counter's variable from the character sheet
    /// (only relevant if it has an associated variable.)
    /// </summary>
    public virtual int? GetStartingValue(ICounterSheet sheet)
    {
        return GetValue(sheet);
    }

    /// <summary>
    /// Gets this counter's value from the sheet.
    /// </summary>
    public virtual int? GetValue(ICounterSheet sheet)
    {
        return sheet.Counters.TryGetValue(Name, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the value of this counter's variable from the character, if there is a variable.
    /// </summary>
    public virtual int? GetVariableValue(ITrackedCharacter character)
    {
        return character.GetVariables().Counters.TryGetValue(Name, out var value) ? value : null;
    }

    /// <summary>
    /// Makes a roll based on this counter.
    /// </summary>
    /// <param name="character">The character to roll for.</param>
    /// <param name="bonus">An optional bonus to add.</param>
    /// <param name="random">The random provider.</param>
    /// <param name="rerolls">The number of times to reroll and take highest, or
    /// lowest (if negative). 0 means roll only once.</param>
    /// <param name="targetValue">An optional target value. If one is not supplied and this
    /// game system implies one, the implied one will be filled in.</param>
    /// <returns>The roll result.</returns>
    /// <exception cref="NotSupportedException">If this counter cannot be rolled.</exception>
    public virtual GameCounterRollResult Roll(
        ITrackedCharacter character,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue)
    {
        throw new NotSupportedException(nameof(Roll));
    }

    public void SetVariableClamped(int newValue, ITrackedCharacter character)
    {
        var maxValue = GetMaxVariableValue(character.Sheet);
        if (maxValue.HasValue)
        {
            newValue = Math.Min(maxValue.Value, Math.Max(minValue, newValue));
        }
        else
        {
            newValue = Math.Max(minValue, newValue);
        }

        var variables = character.GetVariables();
        variables.Counters.SetValue(Name, newValue);
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

        sheet.Counters.SetValue(Name, newValueInt);
        errorMessage = null;
        return true;
    }

    public bool TrySetVariable(int newValue, Adventurer adventurer, [MaybeNullWhen(true)] out string errorMessage)
    {
        var maxValue = GetMaxVariableValue(adventurer.Sheet);
        if (newValue < minValue || newValue > maxValue)
        {
            errorMessage = $"{Name} must be between {minValue} and {maxValue}.";
            return false;
        }

        adventurer.Variables.Counters.SetValue(Name, newValue);
        errorMessage = null;
        return true;
    }
}

public readonly struct GameCounterRollResult
{
    /// <summary>
    /// The number rolled after modifiers.
    /// </summary>
    public int Roll { get; init; }

    /// <summary>
    /// If success is defined, whether or not this roll was successful.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// The roll working.
    /// </summary>
    public string Working { get; init; }
}
