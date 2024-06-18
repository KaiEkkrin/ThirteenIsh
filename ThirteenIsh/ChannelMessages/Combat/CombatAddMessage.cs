namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatAddMessage : CombatMessage
{
    public required string Name { get; init; }
    public int Rerolls { get; init; }
    public int SwarmCount { get; init; }
}
