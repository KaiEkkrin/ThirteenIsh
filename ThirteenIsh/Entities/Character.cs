namespace ThirteenIsh.Entities;

/// <summary>
/// This entity type describes a character, which is owned by a user.
/// </summary>
public class Character : UserEntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The character sheet.
    /// </summary>
    public CharacterSheet Sheet { get; set; } = new();

    /// <summary>
    /// An incrementing version number, used to detect conflicts.
    /// </summary>
    public long Version { get; set; }
}
