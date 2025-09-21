using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatVSetMessage))]
internal sealed class CombatVSetMessageHandler(SqlDataService dataService, DiscordService discordService,
    IRandomWrapper random, IServiceProvider serviceProvider)
    : CombatVSubMessageHandlerBase<CombatVSetMessage>(dataService, discordService, random, serviceProvider)
{
    protected override CombatEditVariableOperation CreateEditOperation(CombatVSetMessage message)
    {
        return new CombatEditVariableOperation(new SetVariableSubOperation(message.VariableNamePart, message.DiceParseTree,
            Random));
    }
}
