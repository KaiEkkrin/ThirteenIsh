using ThirteenIsh.Parsing;

namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatAttackMessage : CombatMessage
{
    public string? Alias { get; init; }
    public required string NamePart { get; init; }
    public ParseTreeBase? Bonus { get; init; }
    public int Rerolls { get; init; }
    public required IReadOnlyCollection<string> Targets { get; init; }
    public required string VsNamePart { get; init; }
}
