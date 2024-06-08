using ThirteenIsh.Parsing;

namespace ThirteenIsh.ChannelMessages.Pcs;

internal class PcVSubMessageBase : GuildMessage
{
    public string? Name { get; init; }
    public required string VariableNamePart { get; init; }
    public required ParseTreeBase DiceParseTree { get; init; }
}
