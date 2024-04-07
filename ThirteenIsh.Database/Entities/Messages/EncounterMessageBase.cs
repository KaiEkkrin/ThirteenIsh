namespace ThirteenIsh.Database.Entities.Messages;

public class EncounterMessageBase : AdventureMessageBase
{
    /// <summary>
    /// The channel ID.
    /// </summary>
    public required ulong ChannelId { get; set; }
}
