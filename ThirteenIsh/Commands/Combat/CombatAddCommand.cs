using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, CharacterType.Monster, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync(
                $"Error getting monster '{name}'. Perhaps they do not exist, or there is more than one character or monster matching that name?",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(channelId, character, random, rerolls, command.User.Id), guildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(message, ephemeral: true);
            return;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        // Update the encounter table
        var encounterTable = output.GameSystem.EncounterTable(output.Adventure, output.Encounter);
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

    private sealed class EditOperation(ulong channelId, Entities.Character character, IRandomWrapper random, int rerolls,
            ulong userId)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            if (!CommandUtil.TryGetCurrentEncounter(guild, channelId, userId, out var adventure,
                out var encounter, out var errorMessage))
            {
                return new MessageEditResult<EditOutput>(null, errorMessage);
            }

            if (adventure.GameSystem != character.GameSystem)
                return new MessageEditResult<EditOutput>(null,
                    "This monster was not created in the same game system as the adventure.");

            var gameSystem = GameSystem.Get(adventure.GameSystem);
            var nameAliasCollection = encounter.BuildNameAliasCollection();
            var result = gameSystem.EncounterAdd(character, encounter, nameAliasCollection, random, rerolls, userId,
                out var alias);

            if (!result.HasValue) return new MessageEditResult<EditOutput>(
                null, $"You are not able to add a '{character.Name}' to this encounter at this time.");

            return new MessageEditResult<EditOutput>(new EditOutput(alias, adventure, encounter, gameSystem, result.Value));
        }
    }

    private sealed record EditOutput(string Alias, Adventure Adventure, Encounter Encounter, GameSystem GameSystem,
        GameCounterRollResult Result);
}
