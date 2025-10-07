using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcListSubCommand(bool asGm)
    : SubCommandBase("list", asGm
        ? "Lists all player characters in the current adventure."
        : "Lists your player characters in the current adventure.")
{
    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var discordService = serviceProvider.GetRequiredService<DiscordService>();

        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        if (await dataService.GetAdventureAsync(guild, null, cancellationToken) is not { } adventure)
        {
            await command.RespondAsync("There is no current adventure.", ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);

        if (asGm)
        {
            // GM mode: Show all adventurers with player annotations
            embedBuilder.WithTitle($"All Characters in {adventure.Name}");

            var adventurers = await dataService.GetAdventurersAsync(adventure)
                .OrderBy(a => a.Name)
                .ToListAsync(cancellationToken);

            if (adventurers.Count == 0)
            {
                await command.RespondAsync("No characters have joined this adventure.", ephemeral: true);
                return;
            }

            foreach (var adventurer in adventurers)
            {
                var guildUser = await discordService.GetGuildUserAsync(guildId, adventurer.UserId);
                var defaultMarker = adventurer.IsDefault ? "⭐ " : "";
                var fieldName = $"{defaultMarker}{adventurer.Name} [{guildUser.DisplayName}]";
                var summary = gameSystem.GetCharacterSummary(adventurer);

                embedBuilder.AddField(new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName(fieldName)
                    .WithValue(summary));
            }
        }
        else
        {
            // Player mode: Show only the user's adventurers
            embedBuilder.WithTitle($"Your Characters in {adventure.Name}");

            var adventurers = await dataService.GetUserAdventurersAsync(adventure, command.User.Id)
                .OrderBy(a => a.Name)
                .ToListAsync(cancellationToken);

            if (adventurers.Count == 0)
            {
                await command.RespondAsync("You have not joined this adventure.", ephemeral: true);
                return;
            }

            foreach (var adventurer in adventurers)
            {
                var defaultMarker = adventurer.IsDefault ? "⭐ " : "";
                var fieldName = $"{defaultMarker}{adventurer.Name}";
                var summary = gameSystem.GetCharacterSummary(adventurer);

                embedBuilder.AddField(new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName(fieldName)
                    .WithValue(summary));
            }
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
