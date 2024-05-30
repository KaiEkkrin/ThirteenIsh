﻿using Discord;
using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

/// <summary>
/// Like PcVSubCommandBase, but applies to any alias during combat.
/// </summary>
internal abstract class CombatVSubCommandBase(bool asGm, string name, string description, string nameOptionDescription,
    string valueOptionDescription)
    : SubCommandBase(name, description)
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("variable-name", ApplicationCommandOptionType.String, nameOptionDescription,
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.String, valueOptionDescription,
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "variable-name", out var namePart))
        {
            await command.RespondAsync("No variable name part supplied.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "value", out var diceString))
        {
            await command.RespondAsync("No value supplied.", ephemeral: true);
            return;
        }

        var parseTree = Parser.Parse(diceString);
        if (!string.IsNullOrEmpty(parseTree.Error))
        {
            await command.RespondAsync(parseTree.Error, ephemeral: true);
            return;
        }

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var editOperation = CreateEditOperation(namePart, parseTree, random);

        var result = await dataService.EditCombatantAsync(
            guildId, channelId, asGm ? null : command.User.Id, editOperation, alias, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                var (adventure, encounter, combatant, character) = output.CombatantResult;

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, guildId,
                    command.Channel, encounter, output.GameSystem, cancellationToken);

                // If this wasn't a simple integer, show the working
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(null, character, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        ExtraFields =
                        [
                            new EmbedFieldBuilder().WithName("Roll").WithValue(output.Working)
                        ],
                        OnlyTheseProperties = [output.GameCounter.Name],
                        OnlyVariables = true,
                        Title = $"Set {output.GameCounter.Name} on {combatant.Alias}"
                    });

                await command.RespondAsync(embed: embed);
            });
    }

    protected abstract CombatEditVariableOperation CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random);
}