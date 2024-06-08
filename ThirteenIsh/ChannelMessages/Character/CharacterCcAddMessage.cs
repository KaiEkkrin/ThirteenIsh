using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.ChannelMessages.Character;

internal sealed class CharacterCcAddMessage : MessageBase
{
    public required CharacterType CharacterType { get; init; }
    public required string Name { get; init; }
    public required string CcName { get; init; }
    public required int DefaultValue { get; init; }
    public required GameCounterOptions GameCounterOptions { get; init; }
}
