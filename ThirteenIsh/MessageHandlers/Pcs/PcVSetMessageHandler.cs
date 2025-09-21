using ThirteenIsh.ChannelMessages.Pcs;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Pcs;

[MessageHandler(MessageType = typeof(PcVSetMessage))]
internal sealed class PcVSetMessageHandler(SqlDataService dataService, IRandomWrapper random)
    : PcVSubMessageHandlerBase<PcVSetMessage>(dataService, random)
{
    protected override PcEditVariableOperation CreateEditOperation(PcVSetMessage message)
    {
        return new PcEditVariableOperation(new SetVariableSubOperation(message.VariableNamePart, message.DiceParseTree, Random));
    }
}
