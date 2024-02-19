using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcRollSubCommand() : SubCommandBase("roll", "Rolls against a player character property.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddRerollsOption("rerolls")
            .AddOption("target-value", ApplicationCommandOptionType.Integer, "The target value.");
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

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;
        int? targetValue = CommandUtil.TryGetOption<int>(option, "target-value", out var t) ? t : null;

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (guild.CurrentAdventure is null ||
            guild.CurrentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer) != true ||
            adventurer is null)
        {
            await command.RespondAsync("Either there is no current adventure or you have not joined it.",
                ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
        var counter = gameSystem.FindCounter(namePart, c => c.Options.HasFlag(GameCounterOptions.CanRoll));
        if (counter is null)
        {
            await command.RespondAsync($"'{namePart}' does not uniquely match a rollable property.",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var result = counter.Roll(adventurer, bonus, random, rerolls, ref targetValue);

        var titleBuilder = new StringBuilder()
            .Append(CultureInfo.CurrentCulture, $"{adventurer.Name} : Rolled {counter.Name}");

        if (targetValue.HasValue)
            titleBuilder.Append(CultureInfo.CurrentCulture, $" vs {targetValue.Value}");

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
