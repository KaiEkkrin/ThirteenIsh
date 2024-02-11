using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Extend this to make the vset and vmod commands since they're extremely similar
/// </summary>
internal abstract class PcVCommandBase(string name, string description,
    string nameOptionDescription, string valueOptionDescription)
    : SubCommandBase(name, description)
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("variable-name", ApplicationCommandOptionType.String, nameOptionDescription)
            .AddOption("value", ApplicationCommandOptionType.String, valueOptionDescription);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
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

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(guildId, cancellationToken);
        if (guild.CurrentAdventure is null ||
            guild.CurrentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer) != true ||
            adventurer is null)
        {
            await command.RespondAsync("Either there is no current adventure or you have not joined it.",
                ephemeral: true);
            return;
        }

        var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
        var counter = gameSystem.FindVariable(namePart);
        if (counter is null)
        {
            await command.RespondAsync($"'{namePart}' does not uniquely match a variable name.",
                ephemeral: true);
            return;
        }

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var editOperation = CreateEditOperation(command, counter, parseTree, random);
        var (result, errorMessage) = await dataService.EditGuildAsync(
            editOperation, guildId, cancellationToken);

        if (errorMessage is not null)
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
            return;
        }

        if (result is null) throw new InvalidOperationException("result was null after update");

        // If this wasn't a simple integer, show the working
        List<EmbedFieldBuilder> extraFields = [];
        if (parseTree is not IntegerParseTree)
        {
            extraFields.Add(new EmbedFieldBuilder()
                .WithName("Roll")
                .WithValue(result.Working));
        }

        var updatedAdventurer = result.Adventure.Adventurers[command.User.Id];
        await CommandUtil.RespondWithAdventurerSummaryAsync(command, updatedAdventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                ExtraFields = extraFields,
                OnlyTheseProperties = new[] { counter.Name },
                OnlyVariables = true,
                Title = $"Set {counter.Name} on {updatedAdventurer.Name}"
            });
    }

    protected abstract VCommandEditOperation CreateEditOperation(SocketSlashCommand command,
        GameCounter counter, ParseTreeBase parseTree, IRandomWrapper random);

    protected abstract class VCommandEditOperation(SocketSlashCommand command)
        : SyncEditOperation<ResultOrMessage<VCommandResult>, Guild, MessageEditResult<VCommandResult>>
    {
        public sealed override MessageEditResult<VCommandResult> DoEdit(Guild guild)
        {
            if (guild.CurrentAdventure is not { } currentAdventure)
                return new MessageEditResult<VCommandResult>(null, "There is no current adventure in this guild.");

            if (!currentAdventure.Adventurers.TryGetValue(command.User.Id, out var adventurer))
                return new MessageEditResult<VCommandResult>(null, "You have not joined the current adventure.");

            return DoEditInternal(currentAdventure, adventurer);
        }

        protected abstract MessageEditResult<VCommandResult> DoEditInternal(Adventure adventure, Adventurer adventurer);
    }

    protected record VCommandResult(Adventure Adventure, string Working);
}
