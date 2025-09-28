namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Base interface for a character, either player or monster.
/// </summary>
public interface ICharacterBase
{
    string Name { get; }

    CharacterSheet Sheet { get; }

    CharacterType Type { get; }

    /// <summary>
    /// The character system name (optional - if null, uses default for CharacterType).
    /// </summary>
    string? CharacterSystemName { get; }

    ulong UserId { get; }

    /// <summary>
    /// Tries to get the value of a fix for this character. Returns false if there is no fix
    /// value or this character does not support fixes.
    /// </summary>
    bool TryGetFix(string name, out int fixValue);
}