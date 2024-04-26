using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
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

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var result = await dataService.EditAdventureAsync(
            guildId, new EditOperation(), name, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            adventure =>
            {
                var discordService = serviceProvider.GetRequiredService<DiscordService>();
                return discordService.RespondWithAdventureSummaryAsync(dataService, command, adventure, name);
            });
    }

    private sealed class EditOperation() : SyncEditOperation<Adventure, Adventure>
    {
        public override EditResult<Adventure> DoEdit(DataContext context, Adventure adventure)
        {
            adventure.Guild.CurrentAdventureName = adventure.Name;
            return new EditResult<Adventure>(adventure);
        }
    }
}
