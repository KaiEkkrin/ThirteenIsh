using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Pcs;

/// <summary>
/// Extend this to make the vset and vmod commands since they're extremely similar
/// These, of course, don't apply to monsters, which don't have an equivalent to "player character" sheets
/// copied into the adventure
/// </summary>
internal abstract class PcVSubCommandBase(bool asGm, string name, string description,
    string nameOptionDescription, string valueOptionDescription)
    : SubCommandBase(name, description)
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOptionIf(asGm, builder => builder.AddOption("name", ApplicationCommandOptionType.String,
                "The character name.", isRequired: true))
            .AddOption("variable-name", ApplicationCommandOptionType.String, nameOptionDescription)
            .AddOption("value", ApplicationCommandOptionType.String, valueOptionDescription);
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

        if (!CommandUtil.TryGetOption<string>(option, "variable-name", out var variableNamePart))
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
        var editOperation = CreateEditOperation(variableNamePart, parseTree, random);

        var result = name != null
            ? await dataService.EditAdventurerAsync(guildId, name, editOperation, cancellationToken)
            : await dataService.EditAdventurerAsync(guildId, command.User.Id, editOperation, cancellationToken);

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

    protected abstract PcEditVariableOperation CreateEditOperation(
        string counterNamePart, ParseTreeBase parseTree, IRandomWrapper random);
}
