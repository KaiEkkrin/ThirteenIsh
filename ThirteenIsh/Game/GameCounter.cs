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
        if (!options.HasFlag(GameCounterOptions.HasVariable))
        {
            return GetValue(character) switch
            {
                { } value => $"{value}",
                _ => Unset
            };
        }

        return (GetMaxVariableValue(character), GetVariableValue(character)) switch
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
    /// (only if it has an associated variable.)
    /// </summary>
    public virtual int? GetMaxVariableValue(ITrackedCharacter character)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable)) return null;
        return GetValue(character);
    }

    /// <summary>
    /// Gets the starting value of this counter's variable from the character sheet
    /// (only if it has an associated variable.)
    /// </summary>
    public virtual int? GetStartingValue(ITrackedCharacter character)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable)) return null;
        return GetValue(character);
    }

    /// <summary>
    /// Gets this counter's value from the sheet.
    /// TODO Change this so that all calls that would have the opportunity to apply fixes
    /// go through that overload.
    /// </summary>
    public virtual int? GetValue(ICounterSheet sheet)
    {
        return sheet.Counters.TryGetValue(Name, out var value) ? value : null;
    }

    /// <summary>
    /// Gets this counter's value for the character, including any fix that applies.
    /// </summary>
    public virtual int? GetValue(ITrackedCharacter character)
    {
        var baseValue = GetValue(character.Sheet);
        return AddFix(baseValue, character);
    }

    /// <summary>
    /// Gets the value of this counter's variable from the character, if there is a variable.
    /// </summary>
    public virtual int? GetVariableValue(ITrackedCharacter character)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable)) return null;
        return character.GetVariables().Counters.TryGetValue(Name, out var value) ? value : GetStartingValue(character);
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

    public void SetFixValue(int newValue, ITrackedCharacter character)
    {
        var fixes = character.GetFixes();
        fixes.Counters.SetValue(Name, newValue);
    }

    public void SetVariableClamped(int newValue, ITrackedCharacter character)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable))
            throw new InvalidOperationException(
                $"Cannot set the variable value of {Name}, this counter does not have a variable.");

        var maxValue = GetMaxVariableValue(character);
        if (maxValue.HasValue)
        {
            newValue = Math.Min(maxValue.Value, newValue);
        }

        newValue = Math.Max(minValue, newValue);
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

    public bool TrySetVariable(int newValue, ITrackedCharacter character, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable))
            throw new InvalidOperationException(
                $"Cannot set the variable value of {Name}, this counter does not have a variable.");

        var maxValue = GetMaxVariableValue(character);
        if (newValue < minValue || newValue > maxValue)
        {
            errorMessage = $"{Name} must be between {minValue} and {maxValue}.";
            return false;
        }

        var variables = character.GetVariables();
        variables.Counters.SetValue(Name, newValue);
        errorMessage = null;
        return true;
    }

    protected int? AddFix(int? baseValue, ITrackedCharacter character)
    {
        return character.GetFixes().Counters.TryGetValue(Name, out var fixValue)
            ? baseValue + fixValue
            : baseValue;
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
