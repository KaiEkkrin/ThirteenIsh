using Discord;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureCommand() : CommandBase("adventure", "Manage adventures.",
        new AdventureAddSubCommand(),
        new AdventureEditSubCommand(),
        new AdventureListSubCommand(),
        new AdventureRemoveSubCommand(),
        new AdventureShowSubCommand(),
        new AdventureSwitchSubCommand())
{
    public override SlashCommandBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .WithDefaultMemberPermissions(GuildPermission.ManageGuild);
    }
}
