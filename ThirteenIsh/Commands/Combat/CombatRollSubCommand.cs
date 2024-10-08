﻿using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// TODO also make a `13-monster roll` for out of combat rolls for monsters?
internal sealed class CombatRollSubCommand() : SubCommandBase("roll", "Rolls against a combatant property.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to roll for.")
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddOption("dc", ApplicationCommandOptionType.Integer, "The amount that counts as a success.")
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "name", out var namePart))
        {
            await command.RespondAsync("No name part supplied.", ephemeral: true);
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

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var combatantResult = await dataService.GetCombatantResultAsync(guild, channelId, command.User.Id, alias,
            cancellationToken);

        await combatantResult.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                var (adventure, encounter, combatant, character) = output;

                var gameSystem = GameSystem.Get(adventure.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(combatant.CharacterType);

                var counter = characterSystem.FindCounter(character.Sheet, namePart,
                    c => c.Options.HasFlag(GameCounterOptions.CanRoll));

                if (counter is null)
                {
                    await command.RespondAsync($"'{namePart}' does not uniquely match a rollable property.",
                        ephemeral: true);
                    return;
                }

                var random = serviceProvider.GetRequiredService<IRandomWrapper>();
                var result = counter.Roll(character, bonus, random, rerolls, ref dc);
                if (result.Error != GameCounterRollError.Success)
                {
                    await command.RespondAsync($"'{namePart}' : {result.ErrorMessage}", ephemeral: true);
                    return;
                }

                var titleBuilder = new StringBuilder()
                    .Append(CultureInfo.CurrentCulture, $"{character.Name} ({combatant.Alias}) : Rolled {counter.Name}");

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
            });
    }
}
