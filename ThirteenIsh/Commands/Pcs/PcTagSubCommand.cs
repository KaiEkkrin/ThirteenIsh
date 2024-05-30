using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

internal sealed class PcTagSubCommand(bool asGm) : SubCommandBase("tag", "Adds a tag to a character.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("tag", ApplicationCommandOptionType.String, "The tag to add", isRequired: true);
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
        AddTagOperation editOperation = new(tagValue);

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
                        Title = $"Added tag '{tagValue}' to {output.Adventurer.Name}"
                    });
            });
    }

    private sealed class AddTagOperation(string tagValue) : SyncEditOperation<AddTagResult, Adventurer>
    {
        public override EditResult<AddTagResult> DoEdit(DataContext context, Adventurer adventurer)
        {
            var gameSystem = GameSystem.Get(adventurer.Adventure.GameSystem);
            if (!adventurer.AddTag(tagValue))
                return CreateError($"Cannot add the tag '{tagValue}' to this adventurer. Perhaps they already have it?");

            return new EditResult<AddTagResult>(new AddTagResult(adventurer, gameSystem));
        }
    }

    private record AddTagResult(Adventurer Adventurer, GameSystem GameSystem);
}
