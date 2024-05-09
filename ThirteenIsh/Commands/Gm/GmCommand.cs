using Discord;

namespace ThirteenIsh.Commands.Gm;

// short for "Game Master" -- game master only commands.
internal sealed class GmCommand() : CommandBase("gm", "Game Master commands.",
    new GmAdventureSubCommandGroup(),
    new GmCombatSubCommandGroup())
{
    public override SlashCommandBuilder CreateBuilder()
    {
        // TODO make it possible to assign GM permission to others
        return base.CreateBuilder()
            .WithDefaultMemberPermissions(GuildPermission.ManageGuild);
    }
}
