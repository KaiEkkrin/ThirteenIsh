using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class ListAdventuresCommand : CommandBase
{
    public ListAdventuresCommand() : base("adventure-list", "Lists adventures")
    {
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithTitle("Adventures");

        foreach (var adventure in guild.Adventures.OrderBy(o => o.Name))
        {
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(adventure.Name)
                .WithValue($"{adventure.Adventurers.Count} adventurers"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
