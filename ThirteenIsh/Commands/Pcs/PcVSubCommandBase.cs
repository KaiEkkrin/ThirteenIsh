using Discord;
using Discord.WebSocket;
using System.Data;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Extend this to make the vset and vmod commands since they're extremely similar
/// These, of course, don't apply to monsters, which don't have an equivalent to "player character" sheets
/// copied into the adventure
/// TODO This isn't working yet in the SQL branch
/// </summary>
internal abstract class PcVSubCommandBase(string name, string description,
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

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        var editOperation = CreateEditOperation(namePart, parseTree, random);

        var (result, errorMessage) = await dataService.EditAdventurerAsync(
            guildId, command.User.Id, editOperation, cancellationToken);

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

        await CommandUtil.RespondWithAdventurerSummaryAsync(command, result.Adventurer, result.GameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                ExtraFields = extraFields,
                OnlyTheseProperties = [result.GameCounter.Name],
                OnlyVariables = true,
                Title = $"Set {result.GameCounter.Name} on {result.Adventurer.Name}"
            });
    }

    protected abstract EditVariableOperationBase CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random);
}
