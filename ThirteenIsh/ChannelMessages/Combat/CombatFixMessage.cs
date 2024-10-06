namespace ThirteenIsh.ChannelMessages.Combat;

internal class CombatFixMessage : CombatMessage
{
    public required bool AsGm { get; init; }
    public string? Alias { get; init; }
    public required string CounterNamePart { get; init; }
    public required int FixValue { get; init; }
}
