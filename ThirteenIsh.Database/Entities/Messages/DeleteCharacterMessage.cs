namespace ThirteenIsh.Database.Entities.Messages;

public class DeleteCharacterMessage : MessageBase
{
    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The character name to delete.
    /// </summary>
    public required string Name { get; set; }
}
