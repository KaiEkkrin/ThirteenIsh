using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Commands.Character;

internal class CharacterCcSubCommandGroup(CharacterType characterType) : SubCommandGroupBase("cc", "Manage custom counters.",
    new CharacterCcAddSubCommand(characterType))
{
}
