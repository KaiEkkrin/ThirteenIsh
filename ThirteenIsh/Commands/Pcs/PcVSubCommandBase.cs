using Discord;
using Discord.WebSocket;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Extend this to make the vset and vmod commands since they're extremely similar
/// These, of course, don't apply to monsters, which don't have an equivalent to "player character" sheets
/// copied into the adventure
/// TODO also make vmod and vset commands for tracked characters in combat -- maybe named `combat-vset` and
/// `combat-vmod`? (would look very similar)
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

        var result = await dataService.EditAdventurerAsync(
            guildId, command.User.Id, editOperation, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            output =>
            {
                // If this wasn't a simple integer, show the working
                List<EmbedFieldBuilder> extraFields = [];
                if (parseTree is not IntegerParseTree)
                {
                    extraFields.Add(new EmbedFieldBuilder()
                        .WithName("Roll")
                        .WithValue(output.Working));
                }

                return CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, output.Adventurer, output.GameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        ExtraFields = extraFields,
                        OnlyTheseProperties = [output.GameCounter.Name],
                        OnlyVariables = true,
                        Title = $"Set {output.GameCounter.Name} on {output.Adventurer.Name}"
                    });
            });
    }

    protected abstract EditVariableOperationBase CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random);
}
