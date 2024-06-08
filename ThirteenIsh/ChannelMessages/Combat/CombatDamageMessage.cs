using ThirteenIsh.Parsing;

namespace ThirteenIsh.ChannelMessages.Combat;

internal sealed class CombatDamageMessage : CombatMessage
{
    public string? Alias { get; init; }
    public string? CounterNamePart { get; init; }
    public required ParseTreeBase DiceParseTree { get; init; }
    public required int Multiplier { get; init; }
    public required bool RollSeparately { get; init; }
    public required IReadOnlyCollection<string> Targets { get; init; }
    public required string VsNamePart { get; init; }
}
