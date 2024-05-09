using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmAdventureSetSubCommand() : SubCommandBase("set", "Sets an adventure property.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.",
                isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "A description of the adventure.",
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

        if (!CommandUtil.TryGetOption<string>(option, "description", out var description)) description = string.Empty;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var result = await dataService.EditAdventureAsync(
            guildId, new EditOperation(description), name, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            adventure =>
            {
                var discordService = serviceProvider.GetRequiredService<DiscordService>();
                return discordService.RespondWithAdventureSummaryAsync(dataService, command, adventure, name);
            });
    }

    private sealed class EditOperation(string description) : SyncEditOperation<Adventure, Adventure>
    {
        public override EditResult<Adventure> DoEdit(DataContext context, Adventure adventure)
        {
            adventure.Description = description;
            return new EditResult<Adventure>(adventure);
        }
    }
}
