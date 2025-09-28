using Discord;
using System.Globalization;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
public class GameCounter(string name, string? alias = null,
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

    public override void AddPropertyValueChoiceOptions(SelectMenuBuilder builder, ICharacterBase character)
    {
        if (!maxValue.HasValue || maxValue.Value - minValue > 25)
            throw new NotSupportedException(
                $"Cannot build property value choice options for {Name} (min = {minValue}, max = {maxValue})");

        var currentValue = GetValue(character);
        for (var i = minValue; i <= maxValue.Value; ++i)
        {
            builder.AddOption($"{i}", $"{i}", isDefault: i == currentValue);
        }
    }

    public override string GetDisplayValue(ICharacterBase character)
    {
        return character is ITrackedCharacter trackedCharacter
            ? GetDisplayValueForTracked(trackedCharacter)
            : GetValue(character) is { } value ? $"{value}" : Unset;
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
    /// Gets this counter's value for the character.
    /// Automatically applies fixes if the character is tracked.
    /// </summary>
    public int? GetValue(ICharacterBase character)
    {
        var baseValue = GetValueInternal(character);
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
    /// <param name="secondCounter">A second counter to use as part of this roll.
    /// Only relevant to SWN skill checks right now.</param>
    /// <param name="flags">Flags that modify how the roll behaves.</param>
    /// <returns>The roll result.</returns>
    public virtual GameCounterRollResult Roll(
        ITrackedCharacter character,
        ParseTreeBase? bonus,
        IRandomWrapper random,
        int rerolls,
        ref int? targetValue,
        GameCounter? secondCounter = null,
        GameCounterRollOptions flags = GameCounterRollOptions.None)
    {
        return new GameCounterRollResult { CounterName = Name, Error = GameCounterRollError.NotRollable };
    }

    public void SetFixValue(int newValue, ITrackedCharacter character)
    {
        var fixes = character.GetFixes();
        if (newValue == 0)
        {
            fixes.Counters.RemoveValue(Name);
        }
        else
        {
            fixes.Counters.SetValue(Name, newValue);
        }
    }

    public void SetVariableClamped(int newValue, ITrackedCharacter character)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable))
            throw new GamePropertyException(
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

    public override bool TryEditCharacterProperty(string newValue, ICharacterBase character,
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

        character.Sheet.Counters.SetValue(Name, newValueInt);
        errorMessage = null;
        return true;
    }

    public bool TrySetVariable(int newValue, ITrackedCharacter character, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (!options.HasFlag(GameCounterOptions.HasVariable))
            throw new GamePropertyException(
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

    /// <summary>
    /// Gets this counter's value for the character. Override to customise how the counter
    /// value is retrieved or calculated.
    /// Does NOT apply fixes -- that's done by GetValue, which calls this method.
    /// </summary>
    protected virtual int? GetValueInternal(ICharacterBase character)
    {
        return character.Sheet.Counters.TryGetValue(Name, out var value) ? value : null;
    }

    private int? AddFix(int? baseValue, ICharacterBase character)
    {
        if (!character.TryGetFix(Name, out var fixValue) || fixValue == 0)
            return baseValue;

        // Ignore fixes if the counter is hidden. These would be unusable (because they wouldn't
        // appear on the character sheet). The Discord-side code should prevent users from setting
        // such fixes.
        if (options.HasFlag(GameCounterOptions.IsHidden))
            return baseValue;

        return baseValue + fixValue;
    }

    private string GetDisplayValueForTracked(ITrackedCharacter character)
    {
        StringBuilder builder = new();
        if (options.HasFlag(GameCounterOptions.HasVariable))
        {
            switch (GetMaxVariableValue(character), GetVariableValue(character))
            {
                case ({ } maxValue, { } varValue):
                    builder.Append(CultureInfo.CurrentCulture, $"{varValue}/{maxValue}");
                    break;

                case ({ } maxValue, null):
                    builder.Append(CultureInfo.CurrentCulture, $"{maxValue}");
                    break;

                default:
                    return Unset;
            }
        }
        else if (GetValue(character) is { } value)
        {
            builder.Append(CultureInfo.CurrentCulture, $"{value}");
        }
        else
        {
            return Unset;
        }

        var fixValue = character.GetFixes().Counters.FirstOrDefault(c => c.Name == Name)?.Value;
        if (fixValue is > 0)
        {
            builder.Append(CultureInfo.CurrentCulture, $" [+{fixValue}]");
        }
        else if (fixValue is < 0)
        {
            builder.Append(CultureInfo.CurrentCulture, $" [{fixValue}]");
        }

        return builder.ToString();
    }
}
