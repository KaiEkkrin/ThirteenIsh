using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class ShowAdventureCommand : CommandBase
{
    public ShowAdventureCommand() : base("adventure-show", "Shows the details of an adventure")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The adventure name");
        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);

        var adventureName = TryGetCanonicalizedMultiPartOption(command.Data, "name", out var name)
            ? name
            : guild.CurrentAdventureName;

        var adventure = guild.Adventures.FirstOrDefault(o => o.Name == adventureName);
        if (adventure is null)
        {
            await command.RespondAsync("No such adventure was found in this guild.");
            return;
        }

        await RespondWithAdventureSummaryAsync(command, adventure, adventure.Name);
    }
}
