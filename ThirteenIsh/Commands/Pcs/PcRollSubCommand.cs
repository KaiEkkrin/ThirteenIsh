using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

// TODO Make an equivalent for rolling with a monster?
internal sealed class PcRollSubCommand() : SubCommandBase("roll", "Rolls against a player character property.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddOption("dc", ApplicationCommandOptionType.Integer, "The amount that counts as a success.")
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetOption<string>(option, "name", out var namePart))
        {
            await command.RespondAsync("No name part supplied.", ephemeral: true);
            return;
        }

        var bonus = GetBonus(option);
        if (!string.IsNullOrEmpty(bonus?.Error))
        {
            await command.RespondAsync(bonus.Error, ephemeral: true);
            return;
        }

        int? dc = CommandUtil.TryGetOption<int>(option, "dc", out var t) ? t : null;
        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (string.IsNullOrEmpty(guild.CurrentAdventureName) ||
            await dataService.GetAdventureAsync(guild, guild.CurrentAdventureName, cancellationToken) is not { } adventure ||
            await dataService.GetAdventurerAsync(adventure, command.User.Id, cancellationToken) is not { } adventurer)
        {
            await command.RespondAsync("Either there is no current adventure or you have not joined it.",
                ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(CharacterType.PlayerCharacter);
        var counter = characterSystem.FindCounter(namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            await command.RespondAsync($"'{namePart}' does not uniquely match a rollable property.",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var result = counter.Roll(adventurer, bonus, random, rerolls, ref dc);

        var titleBuilder = new StringBuilder()
            .Append(CultureInfo.CurrentCulture, $"{adventurer.Name} : Rolled {counter.Name}");

        if (dc.HasValue)
            titleBuilder.Append(CultureInfo.CurrentCulture, $" vs {dc.Value}");

        titleBuilder.Append(CultureInfo.CurrentCulture, $" : {result.Roll}");
        if (result.Success.HasValue)
        {
            var successString = result.Success.Value ? "Success!" : "Failure!";
            titleBuilder.Append(" -- ").Append(successString);
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle(titleBuilder.ToString())
            .WithDescription(result.Working);

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private static ParseTreeBase? GetBonus(SocketSlashCommandDataOption option)
    {
        if (!CommandUtil.TryGetOption<string>(option, "bonus", out var bonusString)) return null;
        return Parser.Parse(bonusString);
    }
}
