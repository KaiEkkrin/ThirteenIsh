namespace ThirteenIsh.Database.Entities.Messages;

public class DeleteCustomCounterMessage : MessageBase
{
    /// <summary>
    /// The name of the custom counter to delete.
    /// </summary>
    public required string CcName { get; set; }

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The character name.
    /// </summary>
    public required string Name { get; set; }
}
