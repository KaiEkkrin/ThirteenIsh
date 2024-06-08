using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.ChannelMessages.Character;

internal sealed class CharacterSetMessage : MessageBase
{
    public required CharacterType CharacterType { get; init; }
    public required string Name { get; init; }
    public required string PropertyName { get; init; }
    public required string NewValue { get; init; }
}
