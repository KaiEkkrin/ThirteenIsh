using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatTagSubCommand(bool asGm) : SubCommandBase("tag", "Adds a tag to a combatant.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to edit.",
                isRequired: asGm)
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to add", isRequired: true);
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
        AddTagOperation editOperation = new(tagValue);

        var result = await dataService.EditCombatantAsync(
            guildId, channelId, asGm ? null : command.User.Id, editOperation, alias, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                await command.DeferAsync();
                var (adventure, encounter, combatant, character) = output.CombatantResult;

                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, guildId,
                    command.Channel, encounter, output.GameSystem, cancellationToken);

                // If this wasn't a simple integer, show the working
                var embed = CommandUtil.BuildTrackedCharacterSummaryEmbed(command, output.CombatantResult.Character,
                    output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Added tag '{tagValue}' to {output.CombatantResult.Combatant.Alias}"
                    });

                await command.ModifyOriginalResponseAsync(properties => properties.Embed = embed);
            });
    }

    private sealed class AddTagOperation(string tagValue) : SyncEditOperation<AddTagResult, CombatantResult>
    {
        public override EditResult<AddTagResult> DoEdit(DataContext context, CombatantResult param)
        {
            var gameSystem = GameSystem.Get(param.Adventure.GameSystem);
            if (!param.Character.AddTag(tagValue))
                return CreateError($"Cannot add the tag '{tagValue}' to this combatant. Perhaps they already have it?");

            return new EditResult<AddTagResult>(new AddTagResult(param, gameSystem));
        }
    }

    private record AddTagResult(CombatantResult CombatantResult, GameSystem GameSystem);
}
