using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Character-related command.
/// </summary>
internal sealed class CharacterCommand() : CommandBase("character", "Manage characters.",
    new CharacterAddSubCommand(CharacterType.PlayerCharacter),
    new CharacterCcSubCommandGroup(CharacterType.PlayerCharacter),
    new CharacterGetSubCommand(CharacterType.PlayerCharacter),
    new CharacterListSubCommand(CharacterType.PlayerCharacter),
    new CharacterRemoveSubCommand(CharacterType.PlayerCharacter),
    new CharacterRollSubCommand(CharacterType.PlayerCharacter),
    new CharacterSetSubCommand(CharacterType.PlayerCharacter))
{
}
