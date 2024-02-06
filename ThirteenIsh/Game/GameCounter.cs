using Discord;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game counter is a numeric value associated with a character, and possibly associated
/// with an adventurer variable.
/// The corresponding entities are a CharacterCounter and an AdventurerVariable.
/// </summary>
internal class GameCounter(string name, string? alias = null, int defaultValue = 0, int minValue = 0,
    int? maxValue = null, bool hasVariable = false)
{
    public string Name => name;

    public string? Alias => alias;

    /// <summary>
    /// True if this counter's value should be stored in the character sheet; false if it
    /// should not be, but instead should be calculated out of other values.
    /// </summary>
    public virtual bool CanStore => true;

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
        string customId, CharacterSheet? sheet, ref int row)
    {
        if (!CanStore) return componentBuilder; // no editing for this one
        var currentValue = sheet != null ? (int?)GetValue(sheet) : null;
        if (maxValue.HasValue && (maxValue - minValue) <= 20)
        {
            // Represent this as a menu. It helps, since Discord doesn't have number
            // input validation
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"{customId}:{name}")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithPlaceholder($"Select a {name} value");

            for (var i = minValue; i <= maxValue.Value; ++i)
            {
                menuBuilder.AddOption($"{i}", $"{i}", isDefault: i == currentValue);
            }

            return componentBuilder.WithSelectMenu(menuBuilder, row++);
        }
        else
        {
            // Sadly this requires a modal instead and would be a massive pain in the butt. :(
            // (Also, discord.net doesn't let me add select menus to modals!)
            throw new NotSupportedException(name);
        }
    }

    /// <summary>
    /// Gets the starting value of this counter's variable from the character sheet
    /// (only relevant if it has an associated variable.)
    /// </summary>
    public virtual int GetStartingValue(CharacterSheet characterSheet)
    {
        return GetValue(characterSheet);
    }

    /// <summary>
    /// Gets this counter's value from the character sheet.
    /// </summary>
    public virtual int GetValue(CharacterSheet characterSheet)
    {
        var counter = characterSheet.Counters.FirstOrDefault(o => o.Name == name);
        return counter != null ? counter.Value : defaultValue;
    }
}

