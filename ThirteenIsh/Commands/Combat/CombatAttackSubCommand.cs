using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// This is like `pc-roll` or `combat-roll`, but instead of rolling against a specified DC, here we roll against
// the attribute of another player (or monster) in the current encounter
internal sealed class CombatAttackSubCommand()
    : SubCommandBase("attack", "Rolls against a player or monster in the encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("counter", ApplicationCommandOptionType.String, "The counter name to roll.",
                isRequired: true)
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to roll for.")
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
        if (!CommandUtil.TryGetOption<string>(option, "counter", out var namePart))
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

                List<CombatantBase> targetCombatants = [];
                if (!CommandUtil.TryFindCombatants(targets, encounter, targetCombatants, out var message))
                {
                    await command.RespondAsync(message, ephemeral: true);
                    return;
                }

                var random = serviceProvider.GetRequiredService<IRandomWrapper>();
                StringBuilder stringBuilder = new();
                SortedSet<string> vsCounterNames = []; // hopefully only one :P
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

                    var vsCharacterSystem = gameSystem.GetCharacterSystem(targetCharacter.Type);
                    var vsCounter = vsCharacterSystem.FindCounter(targetCharacter.Sheet, vsNamePart, _ => true);
                    if (vsCounter is null)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : Target has no counter unambiguously matching '{vsNamePart}'");
                        continue;
                    }

                    vsCounterNames.Add(vsCounter.Name);
                    var dc = vsCounter.GetValue(targetCharacter.Sheet);
                    if (!dc.HasValue)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : Target has no value for {vsCounter.Name}");
                        continue;
                    }

                    var result = counter.Roll(character.Sheet, bonus, random, rerolls, ref dc);
                    stringBuilder.Append(CultureInfo.CurrentCulture, $" ({dc}) : {result.Roll}");
                    if (result.Success.HasValue)
                    {
                        var successString = result.Success.Value ? "Success!" : "Failure!";
                        stringBuilder.Append(" -- ").Append(successString);
                    }
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(result.Working);
                }

                var vsCounterNameSummary = vsCounterNames.Count == 0
                    ? $"'{vsNamePart}'"
                    : string.Join(", ", vsCounterNames);

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithTitle($"{character.Name} : Rolled {counter.Name} vs {vsCounterNameSummary}")
                    .WithDescription(stringBuilder.ToString());

                await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: vsCounterNames.Count == 0);
            });
    }
}
