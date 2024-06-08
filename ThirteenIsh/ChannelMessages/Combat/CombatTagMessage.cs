namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatTagMessage : CombatMessage
{
    public required bool AsGm { get; init; }
    public string? Alias { get; init; }
    public required string TagValue { get; init; }
}
