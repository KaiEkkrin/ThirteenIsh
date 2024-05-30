using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcUntagSubCommand(bool asGm) : SubCommandBase("untag", "Removes a tag from an adventurer.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to remove", isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;

        string? name = null;
        if (asGm && !CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out name))
        {
            await command.RespondAsync(
                $"A valid {CharacterType.PlayerCharacter.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} name must be supplied.",
                ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetTagOption(option, "tag", out var tagValue))
        {
            await command.RespondAsync("A valid tag value is required.", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        RemoveTagOperation editOperation = new(tagValue);

        var result = name != null
            ? await dataService.EditAdventurerAsync(guildId, name, editOperation, cancellationToken)
            : await dataService.EditAdventurerAsync(guildId, command.User.Id, editOperation, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            output =>
            {
                return CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        OnlyTheseProperties = [],
                        Flags = CommandUtil.AdventurerSummaryFlags.OnlyVariables | CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = $"Removed tag '{tagValue}' from {output.Adventurer.Name}"
                    });
            });
    }

    private sealed class RemoveTagOperation(string tagValue) : SyncEditOperation<RemoveTagResult, Adventurer>
    {
        public override EditResult<RemoveTagResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            if (!adventurer.RemoveTag(tagValue))
                return CreateError($"Cannot remove the tag '{tagValue}' from this adventurer. Perhaps they do not have it?");

            return new EditResult<RemoveTagResult>(new RemoveTagResult(adventurer, gameSystem));
        }
    }

    private record RemoveTagResult(Adventurer Adventurer, GameSystem GameSystem);
}
