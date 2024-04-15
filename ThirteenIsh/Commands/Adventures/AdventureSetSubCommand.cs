using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureSetSubCommand() : SubCommandBase("set", "Sets an adventure property.")
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
        var (result, message) = await dataService.EditGuildAsync(
            new EditOperation(dataService, name, description), guildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (result is null) throw new InvalidOperationException(nameof(result));

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(dataService, command, result.Guild, result.Adventure, name);
    }

    private sealed class EditOperation(SqlDataService dataService, string name, string description)
        : EditOperation<ResultOrMessage<AdventureResult>, Guild, MessageEditResult<AdventureResult>>
    {
        public override async Task<MessageEditResult<AdventureResult>> DoEditAsync(DataContext context, Guild guild,
            CancellationToken cancellationToken = default)
        {
            var adventure = await dataService.GetAdventureAsync(guild, name, cancellationToken);
            if (adventure == null) return new MessageEditResult<AdventureResult>(
                null, $"No adventure found matching name '{name}'.");

            adventure.Description = description;
            return new MessageEditResult<AdventureResult>(new AdventureResult(guild, adventure));
        }
    }
}
