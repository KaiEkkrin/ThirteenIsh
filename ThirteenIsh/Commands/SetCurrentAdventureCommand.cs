using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class SetCurrentAdventureCommand : CommandBase
{
    public SetCurrentAdventureCommand() : base("adventure-set-current", "Sets the current adventure")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The adventure name",
            isRequired: true);

        builder.WithDefaultMemberPermissions(GuildPermission.ManageGuild);
        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.SetCurrentAdventureAsync(name, guildId, cancellationToken);
        if (guild?.CurrentAdventure is null)
        {
            await command.RespondAsync($"No such adventure '{name}'", ephemeral: true);
            return;
        }

        await CommandUtil.RespondWithAdventureSummaryAsync(command, guild.CurrentAdventure, name);
    }
}
