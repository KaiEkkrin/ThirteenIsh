using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmCombatRemoveSubCommand() : SubCommandBase("remove", "Removes a combatant from the current encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The alias of the combatant to remove.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;
        if (!CommandUtil.TryGetOption<string>(option, "alias", out var alias))
        {
            await command.RespondAsync("No alias supplied.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var result = await dataService.EditEncounterAsync(
            guildId, channelId, new EditOperation(alias), cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            async output =>
            {
                // Update the encounter table
                var encounterTable = await CommandUtil.UpdateEncounterMessageAsync(serviceProvider, guildId,
                    command.Channel, output.Encounter, output.GameSystem, cancellationToken);

                // Send a response
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(command.User)
                    .WithTitle($"'{alias}' was removed from the combat.")
                    .WithDescription(encounterTable);

                await command.RespondAsync(embed: embedBuilder.Build());
            });
    }

    private sealed class EditOperation(string alias)
        : SyncEditOperation<EditOutput, EncounterResult>
    {
        public override EditResult<EditOutput> DoEdit(DataContext context, EncounterResult result)
        {
            var (adventure, encounter) = result;

            var toRemove = encounter.Combatants.SingleOrDefault(c => c.Alias == alias);
            if (toRemove == null)
                return CreateError($"There is no combatant matching alias '{alias}'.");

            if (encounter.GetCurrentCombatant()?.Alias == toRemove.Alias)
                return CreateError($"'{alias}' cannot be removed, because it is currently their turn.");

            encounter.Combatants.Remove(toRemove);
            return new EditResult<EditOutput>(new EditOutput(adventure, encounter, GameSystem.Get(adventure.GameSystem)));
        }
    }

    private sealed record EditOutput(Adventure Adventure, Encounter Encounter, GameSystem GameSystem);
}
