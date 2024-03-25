using Discord;

namespace ThirteenIsh.Commands.Adventures;

// TODO overly long command name "adventure" -- change to "gm"? (Since this is where game master commands go)
internal sealed class AdventureCommand() : CommandBase("adventure", "Manage adventures.",
        new AdventureAddSubCommand(),
        new AdventureEncounterSubCommandGroup(),
        new AdventureGetSubCommand(),
        new AdventureListSubCommand(),
        new AdventureRemoveSubCommand(),
        new AdventureSetSubCommand(),
        new AdventureSwitchSubCommand())
{
    public override SlashCommandBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .WithDefaultMemberPermissions(GuildPermission.ManageGuild);
    }
}
