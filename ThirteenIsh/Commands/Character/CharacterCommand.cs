using ThirteenIsh.Entities;

namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Character-related command.
/// </summary>
internal sealed class CharacterCommand() : CommandBase("character", "Manage characters.",
    new CharacterAddSubCommand(CharacterType.PlayerCharacter),
    new CharacterGetSubCommand(CharacterType.PlayerCharacter),
    new CharacterListSubCommand(CharacterType.PlayerCharacter),
    new CharacterRemoveSubCommand(CharacterType.PlayerCharacter),
    new CharacterSetSubCommand(CharacterType.PlayerCharacter))
{
}
