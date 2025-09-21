namespace ThirteenIsh.Database.Entities.Messages;

public class ListCharactersMessage : MessageBase
{
    public const string DoneControlId = "Done";
    public const string MoreControlId = "More";

    /// <summary>
    /// The character type.
    /// </summary>
    public required CharacterType CharacterType { get; set; }

    /// <summary>
    /// The character name to start listing from.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The page size to use.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// If true, list characters after but not including the named one.
    /// </summary>
    public bool After { get; set; }
}

