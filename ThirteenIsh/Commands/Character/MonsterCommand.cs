using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Monster-related command (the same as the character command, but
/// using the monster type.)
/// </summary>
internal sealed class MonsterCommand() : CommandBase("monster", "Manage monsters.",
    new CharacterAddSubCommand(CharacterType.Monster),
    new CharacterCcSubCommandGroup(CharacterType.Monster),
    new CharacterGetSubCommand(CharacterType.Monster),
    new CharacterListSubCommand(CharacterType.Monster),
    new CharacterRemoveSubCommand(CharacterType.Monster),
    new CharacterSetSubCommand(CharacterType.Monster))
{
}
