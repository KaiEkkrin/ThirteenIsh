using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatUntagSubCommand(bool asGm) : SubCommandBase("untag", "Removes a tag from a combatant.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to remove", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        if (!CommandUtil.TryGetTagOption(option, "tag", out var tagValue))
        {
            await command.RespondAsync("A valid tag value is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        RemoveTagOperation editOperation = new(tagValue);

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
                await CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, output.CombatantResult.Character,
                    output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Removed tag '{tagValue}' from {output.CombatantResult.Combatant.Alias}"
                    });
            });
    }

    private sealed class RemoveTagOperation(string tagValue) : SyncEditOperation<RemoveTagResult, CombatantResult>
    {
        public override EditResult<RemoveTagResult> DoEdit(DataContext context, CombatantResult param)
        {
            var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
            if (!param.Character.RemoveTag(tagValue))
                return CreateError($"Cannot remove the tag '{tagValue}' from this combatant. Perhaps they do not have it?");

            return new EditResult<RemoveTagResult>(new RemoveTagResult(param, gameSystem));
        }
    }

    private record RemoveTagResult(CombatantResult CombatantResult, GameSystem GameSystem);
}
