using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterRollSubCommand(CharacterType characterType)
    : SubCommandBase("roll", $"Rolls against a {characterType.FriendlyName()} property.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The monster name.", isRequired: true)
            .AddOption("counter", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddOption("dc", ApplicationCommandOptionType.Integer, "The amount that counts as a success.")
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces",
                ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "counter", out var counterNamePart))
        {
            await command.RespondAsync("No counter name part supplied.", ephemeral: true);
            return;
        }

        var bonus = CommandUtil.GetBonus(option);
        if (!string.IsNullOrEmpty(bonus?.Error))
        {
            await command.RespondAsync(bonus.Error, ephemeral: true);
            return;
        }

        int? dc = CommandUtil.TryGetOption<int>(option, "dc", out var t) ? t : null;
        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, characterType,
            false, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting {characterType.FriendlyName()} '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(character.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(characterType);
        var counter = characterSystem.FindCounter(character.Sheet, counterNamePart,
            c => c.Options.HasFlag(GameCounterOptions.CanRoll));

        if (counter is null)
        {
            await command.RespondAsync($"'{counterNamePart}' does not uniquely match a rollable property.",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var result = counter.Roll(character.Sheet, bonus, random, rerolls, ref dc);

        var titleBuilder = new StringBuilder()
            .Append(CultureInfo.CurrentCulture, $"{character.Name} : Rolled {counter.Name}");

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
}
