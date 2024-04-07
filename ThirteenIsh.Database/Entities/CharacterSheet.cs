namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Defines a character sheet.
/// </summary>
public class CharacterSheet
{
    public List<CharacterCounter> Counters { get; set; } = [];

    public List<CharacterProperty> Properties { get; set; } = [];
}

public record CharacterProperty(string Name, string Value);

public record CharacterCounter(string Name, int Value);

