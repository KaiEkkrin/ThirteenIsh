namespace ThirteenIsh.Database.Entities.Messages;

public class AdventureMessageBase : MessageBase
{
    /// <summary>
    /// The guild ID.
    /// </summary>
    public required ulong GuildId { get; set; }

    /// <summary>
    /// The adventure name.
    /// </summary>
    public required string Name { get; set; }
}
