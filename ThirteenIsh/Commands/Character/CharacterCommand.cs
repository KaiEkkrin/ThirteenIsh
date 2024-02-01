namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Character-related command.
/// </summary>
internal sealed class CharacterCommand() : CommandBase("character", "Manage characters.",
        new SubCommandGroupBase("ability", "Manage a character's ability scores.",
            new CharacterAbilityGetSubCommand(),
            new CharacterAbilityRollSubCommand(),
            new CharacterAbilitySetSubCommand()),
        new SubCommandGroupBase("level", "Manage a character's level.",
            new CharacterLevelGetSubCommand(),
            new CharacterLevelSetSubCommand()),
        new SubCommandGroupBase("list", "Manage your characters.",
            new CharacterListAddSubCommand(),
            new CharacterListGetSubCommand(),
            new CharacterListRemoveSubCommand()),
        new SubCommandGroupBase("sheet", "Manage your character's sheet.",
            new CharacterSheetGetSubCommand()))
{
}
