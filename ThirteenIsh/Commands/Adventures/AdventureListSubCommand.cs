using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Adventures;

internal sealed class AdventureListSubCommand() : SubCommandBase("list", "Lists the adventures in this guild.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithTitle("Adventures");

        await foreach (var adventure in dataService.GetAdventuresAsync(guild))
        {
            var nameStringBuilder = new StringBuilder()
                .Append(CultureInfo.CurrentCulture, $"[{adventure.GameSystem}] {adventure.Name}");

            if (adventure.Name == guild.CurrentAdventureName)
                nameStringBuilder.Append(" [Current]");

            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName(nameStringBuilder.ToString())
                .WithValue($"{adventure.Adventurers.Count} adventurers"));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
