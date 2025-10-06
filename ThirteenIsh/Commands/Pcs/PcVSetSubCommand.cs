using ThirteenIsh.ChannelMessages;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcVSetSubCommand(bool asGm) : PcVSubCommandBase(asGm, "vset", "Sets a variable value.",
    "The variable name to set.", "A number or dice expression to set it to.")
{
    protected override PcVSubMessageBase BuildMessage(ulong guildId, bool asGm, ulong userId, string? name, string variableNamePart,
        ParseTreeBase diceParseTree)
    {
        return new PcVSetMessage
        {
            GuildId = guildId,
            AsGm = asGm,
            UserId = userId,
            Name = name,
            VariableNamePart = variableNamePart,
            DiceParseTree = diceParseTree
        };
    }
}
