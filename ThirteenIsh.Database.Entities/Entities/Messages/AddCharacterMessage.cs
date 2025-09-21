namespace ThirteenIsh.Database.Entities.Messages;

public class AddCharacterMessage : MessageBase
{
    public const string CancelControlId = "Cancel";
    public const string DoneControlId = "Done";

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The character name to edit.
    /// </summary>
    public required string Name { get; set; }
}
