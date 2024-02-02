using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureSwitchSubCommand() : SubCommandBase("switch", "Sets the currently active adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var updatedGuild = await dataService.EditGuildAsync(
            guild =>
            {
                var adventure = guild.Adventures.FirstOrDefault(o => o.Name == name);
                if (adventure is null) return new EditResult<Guild>(null); // no such adventure

                guild.CurrentAdventureName = name;
                return new EditResult<Guild>(guild);
            },
            guildId, cancellationToken);

        if (updatedGuild?.CurrentAdventure is null)
        {
            await command.RespondAsync($"No such adventure '{name}'", ephemeral: true);
            return;
        }

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(command, updatedGuild, updatedGuild.CurrentAdventure, name);
    }
}
