using ThirteenIsh.ChannelMessages;
using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModSubCommand(bool asGm) : PcVSubCommandBase(asGm, "vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override PcVSubMessageBase BuildMessage(ulong guildId, bool asGm, ulong userId, string? name, string variableNamePart,
        ParseTreeBase diceParseTree)
    {
        return new PcVModMessage
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
