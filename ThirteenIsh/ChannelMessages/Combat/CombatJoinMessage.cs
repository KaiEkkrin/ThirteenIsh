namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatJoinMessage : CombatMessage
{
    public int Rerolls { get; init; }
    public string? Name { get; init; }
}
