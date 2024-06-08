using ThirteenIsh.Parsing;

namespace ThirteenIsh.ChannelMessages.Combat;

internal class CombatVSubMessageBase : CombatMessage
{
    public required bool AsGm { get; init; }
    public string? Alias { get; init; }
    public required string VariableNamePart { get; init; }
    public required ParseTreeBase DiceParseTree { get; init; }
}
