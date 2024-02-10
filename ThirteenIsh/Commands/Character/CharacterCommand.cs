namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Character-related command.
/// </summary>
internal sealed class CharacterCommand() : CommandBase("character", "Manage characters.",
    new CharacterAddCommand(),
    new CharacterGetCommand(),
    new CharacterRemoveSubCommand())
{
}
