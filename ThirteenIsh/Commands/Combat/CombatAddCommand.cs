using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Results;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

// Use `pc combat join` to join a combat as an adventurer -- done like that so only one
// copy of each adventurer can go into a combat (otherwise things would get very weird)!
internal sealed class CombatAddCommand() : SubCommandBase("add", "Adds a monster to the current encounter.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The monster name.", isRequired: true)
            .AddRerollsOption("rerolls");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                "Monster names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "rerolls", out var rerolls)) rerolls = 0;

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, CharacterType.Monster, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting monster '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var (output, message) = await dataService.EditEncounterAsync(guildId, channelId,
            new EditOperation(character, random, rerolls, command.User.Id), cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        // Update the encounter table
        var encounterTable = await output.GameSystem.BuildEncounterTableAsync(dataService, output.Adventure,
            output.Encounter, cancellationToken);

        var pinnedMessageService = serviceProvider.GetRequiredService<PinnedMessageService>();
        await pinnedMessageService.SetEncounterMessageAsync(command.Channel, output.Encounter.AdventureName, guildId,
            encounterTable, cancellationToken);

        // Send an appropriate response
        var embedBuilder = new EmbedBuilder()
            .WithAuthor(command.User)
            .WithTitle($"A {character.Name} joined the encounter as {output.Alias} : {output.Result.Roll}")
            .WithDescription($"{output.Result.Working}\n{encounterTable}");

        await command.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(Database.Entities.Character character,
        IRandomWrapper random, int rerolls, ulong userId)
        : SyncEditOperation<ResultOrMessage<EditOutput>, EncounterResult, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(DataContext context, EncounterResult encounterResult)
        {
            var (adventure, encounter) = encounterResult;
            if (adventure.GameSystem != character.GameSystem)
                return new MessageEditResult<EditOutput>(null,
                    "This monster was not created in the same game system as the adventure.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            NameAliasCollection nameAliasCollection = new(encounter);
            var result = gameSystem.EncounterAdd(context, character, encounter, nameAliasCollection,
                random, rerolls, userId, out var alias);

            if (!result.HasValue) return new MessageEditResult<EditOutput>(
                null, $"You are not able to add a '{character.Name}' to this encounter at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(alias, adventure, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(string Alias, Adventure Adventure, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
