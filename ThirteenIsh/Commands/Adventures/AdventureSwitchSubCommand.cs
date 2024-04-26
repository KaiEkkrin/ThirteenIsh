using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Results;
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
        var (adventure, message) = await dataService.EditAdventureAsync(
            guildId, new EditOperation(), name, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (adventure is null) throw new InvalidOperationException(nameof(adventure));

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(dataService, command, adventure, name);
    }

    private sealed class EditOperation()
        : SyncEditOperation<ResultOrMessage<Adventure>, Adventure, MessageEditResult<Adventure>>
    {
        public override MessageEditResult<Adventure> DoEdit(DataContext context, Adventure adventure)
        {
            adventure.Guild.CurrentAdventureName = adventure.Name;
            return new MessageEditResult<Adventure>(adventure);
        }
    }
}
