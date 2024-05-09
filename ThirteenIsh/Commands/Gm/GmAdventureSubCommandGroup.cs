namespace ThirteenIsh.Commands.Gm;

/// <summary>
/// GM-only commands for managing adventures go here.
/// </summary>
internal class GmAdventureSubCommandGroup() : SubCommandGroupBase("adventure", "Manage adventures.",
    new GmAdventureAddSubCommand(),
    new GmAdventureGetSubCommand(),
    new GmAdventureListSubCommand(),
    new GmAdventureRemoveSubCommand(),
    new GmAdventureSetSubCommand(),
    new GmAdventureSwitchSubCommand())
{
}
