using Discord;
using Polly.Caching;
using System.Globalization;
using System.Text;
using ThirteenIsh.ChannelMessages.Combat;
using ThirteenIsh.Commands;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers.Combat;

[MessageHandler(MessageType = typeof(CombatAttackMessage))]
internal sealed class CombatAttackMessageHandler(SqlDataService dataService, DiscordService discordService,
    IRandomWrapper random) : MessageHandlerBase<CombatAttackMessage>
{
    protected override async Task<bool> HandleInternalAsync(IDiscordInteraction interaction, string controlId,
        CombatAttackMessage message, CancellationToken cancellationToken = default)
    {
        var guild = await dataService.GetGuildAsync(message.GuildId, cancellationToken);
        var combatantResult = await dataService.GetCombatantResultAsync(guild, message.ChannelId, message.UserId,
            message.Alias, cancellationToken);

        await combatantResult.Handle(
            errorMessage => interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage),
            async output =>
            {
                var (adventure, encounter, combatant, character) = output;

                var gameSystem = GameSystem.Get(adventure.GameSystem);
                var characterSystem = gameSystem.GetCharacterSystem(combatant.CharacterType, character.CharacterSystemName);
                var counter = characterSystem.FindCounter(character.Sheet, message.NamePart,
                    c => c.Options.HasFlag(GameCounterOptions.CanRoll));

                if (counter is null)
                {
                    await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                        $"'{message.NamePart}' does not uniquely match a rollable property.");

                    return;
                }

                GameCounter? secondaryCounter = null;
                if (!string.IsNullOrWhiteSpace(message.SecondaryNamePart))
                {
                    secondaryCounter = characterSystem.FindCounter(character.Sheet, message.SecondaryNamePart,
                        c => c.Options.HasFlag(GameCounterOptions.CanRoll));

                    if (secondaryCounter is null)
                    {
                        await interaction.ModifyOriginalResponseAsync(properties => properties.Content =
                            $"'{message.SecondaryNamePart}' does not uniquely match a rollable property.");

                        return;
                    }
                }

                var attackBonus = characterSystem.GetAttackBonus(character, encounter, message.Bonus);

                List<CombatantBase> targetCombatants = [];
                if (!CommandUtil.TryFindCombatants(message.Targets, encounter, targetCombatants, out var errorMessage))
                {
                    await interaction.ModifyOriginalResponseAsync(properties => properties.Content = errorMessage);
                    return;
                }

                StringBuilder stringBuilder = new();
                SortedSet<string> vsCounterNames = []; // hopefully only one :P
                for (var i = 0; i < targetCombatants.Count; ++i)
                {
                    if (i > 0) stringBuilder.AppendLine(); // space things out

                    var target = targetCombatants[i];
                    stringBuilder.Append(CultureInfo.CurrentCulture, $"vs {target.Alias}");
                    var targetCharacter = await dataService.GetCharacterAsync(target, encounter, cancellationToken);
                    if (targetCharacter is null)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture, $" : Target unresolved");
                        continue;
                    }

                    var vsCharacterSystem = gameSystem.GetCharacterSystem(targetCharacter.Type, targetCharacter.CharacterSystemName);
                    var vsCounter = vsCharacterSystem.FindCounter(targetCharacter.Sheet, message.VsNamePart, _ => true);
                    if (vsCounter is null)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : Target has no counter unambiguously matching '{message.VsNamePart}'");
                        continue;
                    }

                    vsCounterNames.Add(vsCounter.Name);
                    var dc = vsCounter.GetValue(targetCharacter);
                    if (!dc.HasValue)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : Target has no value for {vsCounter.Name}");
                        continue;
                    }

                    var result = counter.Roll(character, attackBonus, random, message.Rerolls, ref dc, secondaryCounter, GameCounterRollOptions.IsAttack);
                    if (result.Error != GameCounterRollError.Success)
                    {
                        stringBuilder.AppendLine(CultureInfo.CurrentCulture,
                            $" : {vsCounter.Name} : {result.ErrorMessage}");
                        continue;
                    }

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
                    ? $"'{message.VsNamePart}'"
                    : string.Join(", ", vsCounterNames);

                var user = await discordService.GetGuildUserAsync(message.GuildId, message.UserId);

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithTitle($"{character.Name} : Rolled {counter.Name} vs {vsCounterNameSummary}")
                    .WithDescription(stringBuilder.ToString());

                await interaction.ModifyOriginalResponseAsync(properties => properties.Embed = embedBuilder.Build());
            });

        return true;
    }
}
