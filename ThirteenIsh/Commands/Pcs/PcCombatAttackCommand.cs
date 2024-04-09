using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

// This is like `pc-roll`, but instead of rolling against a specified DC, here we roll against
// the attribute of another player (or monster) in the current encounter
// TODO Make an equivalent for attacking with a monster? (with an optional property name, since
// monsters often have ad hoc attacks?)
// (or move this stuff to just `13-combat` and have it work off of the current combatant,
// or a named one, without necessarily being bound to the PC?)
// TODO maybe change this to `13-combat attack`, have it use the current combatant (if it's
// added with your user ID) or the named combatant if an optional name is supplied (again if it's
// added with your user ID.)
internal sealed class PcCombatAttackCommand()
    : SubCommandBase("attack", "Rolls against a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The property name to roll.",
                isRequired: true)
            .AddOption("bonus", ApplicationCommandOptionType.String, "A bonus dice expression to add.")
            .AddRerollsOption("rerolls")
            .AddOption("target", ApplicationCommandOptionType.String,
                "The target(s) in the current encounter (comma separated). Specify `vs` and the counter targeted.",
                isRequired: true)
            .AddOption("vs", ApplicationCommandOptionType.String, "The counter targeted.", isRequired: true);
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

        var bonus = GetBonus(option);
        if (!string.IsNullOrEmpty(bonus?.Error))
        {
            await command.RespondAsync(bonus.Error, ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var targets = CommandUtil.TryGetOption<string>(option, "target", out var targetString)
            ? targetString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;
        if (targets is not { Length: > 0 })
        {
            await command.RespondAsync("No target supplied.", ephemeral: true);
            return;
        }

        var vsNamePart = CommandUtil.TryGetOption<string>(option, "vs", out var vsString) ? vsString : null;
        if (string.IsNullOrWhiteSpace(vsNamePart))
        {
            await command.RespondAsync("No vs supplied.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        var encounterResult = await dataService.GetEncounterResultAsync(guild, channelId, cancellationToken);
        if (!string.IsNullOrEmpty(encounterResult.Value.ErrorMessage))
        {
            await command.RespondAsync(encounterResult.Value.ErrorMessage, ephemeral: true);
            return;
        }

        var (adventure, encounter) = encounterResult.Value.Value ?? throw new InvalidOperationException(
            "GetEncounterResultAsync did not return a value");

        var combatant = encounter.Combatants.FirstOrDefault(c => c.CharacterType == CharacterType.PlayerCharacter &&
                                                                 c.UserId == command.User.Id);
        if (combatant == null ||
            await dataService.GetCharacterAsync(combatant, cancellationToken) is not { } character)
        {
            await command.RespondAsync("You have not joined this encounter.", ephemeral: true);
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

        List<CombatantBase> targetCombatants = [];
        if (!CommandUtil.TryFindCombatants(targets, encounter, targetCombatants, out var errorMessage))
        {
            await command.RespondAsync(errorMessage);
            return;
        }

        var vsCounterByType = CommandUtil.FindCounterByType(gameSystem, vsNamePart, _ => true, targetCombatants);
        if (vsCounterByType.Count == 0)
        {
            await command.RespondAsync($"'{vsNamePart}' does not uniquely match a variable property.", ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        StringBuilder stringBuilder = new();
        for (var i = 0; i < targetCombatants.Count; ++i)
        {
            if (i > 0) stringBuilder.AppendLine(); // space things out

            var target = targetCombatants[i];
            stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Alias}");
            var targetCharacter = await dataService.GetCharacterAsync(target, cancellationToken);
            if (targetCharacter is null)
            {
                stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : Target unresolved");
                continue;
            }

            var vsCounter = vsCounterByType[CharacterType.PlayerCharacter];
            var dc = vsCounter.GetValue(targetCharacter.Sheet);
            if (!dc.HasValue)
            {
                stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : Target has no {vsCounter.Name}");
                continue;
            }

            var result = counter.Roll(character, bonus, random, rerolls, ref dc);
            stringBuilder.Append(CultureInfo.CurrentCulture, $" ({dc}) : {result.Roll}");
            if (result.Success.HasValue)
            {
                var successString = result.Success.Value ? "Success!" : "Failure!";
                stringBuilder.Append(" -- ").Append(successString);
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(result.Working);
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"{character.Name} : Rolled {counter.Name} vs {vsCounterByType.Values.First().Name}")
            .WithDescription(stringBuilder.ToString());

        await command.RespondAsync(embed: embedBuilder.Build());
        return;
    }

    private static ParseTreeBase? GetBonus(SocketSlashCommandDataOption option)
    {
        if (!CommandUtil.TryGetOption<string>(option, "bonus", out var bonusString)) return null;
        return Parser.Parse(bonusString);
    }
}
