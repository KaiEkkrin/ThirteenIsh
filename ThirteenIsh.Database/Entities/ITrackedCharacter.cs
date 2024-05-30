namespace ThirteenIsh.Database.Entities;

/// <summary>
/// Encompasses any tracked character with variables, which can either be a player's adventurer
/// or a monster within an encounter.
/// </summary>
public interface ITrackedCharacter
{
    public string Name { get; }

    public DateTimeOffset LastUpdated { get; set; }

    public CharacterSheet Sheet { get; }

    public CharacterType Type { get; }

    public ulong UserId { get; }

    public VariablesSheet GetVariables();
}
