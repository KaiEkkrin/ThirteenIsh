using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Commands;
using ThirteenIsh.EditOperations;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

public class EncounterDamageMessage : MessageBase
{
    public const string TakeFullControlId = "Full";
    public const string TakeHalfControlId = "Half";
    public const string TakeNoneControlId = "None";

    /// <summary>
    /// The amount of damage.
    /// TODO Support dealing damage to monsters. Right now, I'm just going to deal damage to
    /// the user's current adventurer.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// The guild ID. (This won't be in the message context, because this message is sent as a DM.)
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    /// <summary>
    /// The variable to apply the damage to.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var guild = await dataService.EnsureGuildAsync(NativeGuildId, cancellationToken);
        if (guild.CurrentAdventure is null ||
            guild.CurrentAdventure.Adventurers.TryGetValue(component.User.Id, out var adventurer) != true ||
            adventurer is null)
        {
            await component.RespondAsync("Either there is no current adventure or you have not joined it.",
                ephemeral: true);
            return true;
        }

        var gameSystem = GameSystem.Get(guild.CurrentAdventure.GameSystem);
        var counter = gameSystem.FindCounter(VariableName, c => c.Options.HasFlag(GameCounterOptions.HasVariable));
        if (counter is null)
        {
            await component.RespondAsync($"'{VariableName}' does not uniquely match a variable name.",
                ephemeral: true);
            return true;
        }

        // Illustrating this as a parse tree should make it clearer what has happened
        var totalDamage = GetDamageAndParseTree(controlId, out var parseTree);

        var random = serviceProvider.GetRequiredService<IRandomWrapper>();
        ModVariableOperation editOperation = new(component, counter, parseTree, random);
        var (result, errorMessage) = await dataService.EditGuildAsync(editOperation, NativeGuildId, cancellationToken);

        if (errorMessage is not null)
        {
            await component.RespondAsync(errorMessage, ephemeral: true);
            return true;
        }

        if (result is null) throw new InvalidOperationException("result was null after update");

        var updatedAdventurer = result.Adventure.Adventurers[component.User.Id];
        var title = totalDamage <= 0
            ? $"{updatedAdventurer.Name} lost {-totalDamage} {counter.Name}"
            : $"{updatedAdventurer.Name} gained {totalDamage} {counter.Name}";

        // TODO Publish a message to the guild too

        List<EmbedFieldBuilder> extraFields = [];
        if (parseTree is not IntegerParseTree)
        {
            extraFields.Add(new EmbedFieldBuilder()
                .WithName("Damage")
                .WithValue(result.Working));
        }

        await CommandUtil.RespondWithAdventurerSummaryAsync(component, updatedAdventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                ExtraFields = extraFields,
                OnlyTheseProperties = [counter.Name],
                OnlyVariables = true,
                Title = title
            });

        return true;
    }

    private int GetDamageAndParseTree(string controlId, out ParseTreeBase parseTree)
    {
        switch (controlId)
        {
            case TakeHalfControlId:
                parseTree = new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, Damage),
                    new IntegerParseTree(0, 2),
                    '/');
                return Damage / 2;

            case TakeNoneControlId:
                parseTree = new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, Damage),
                    new IntegerParseTree(0, 0),
                    '*');
                return 0;

            default:
                parseTree = new IntegerParseTree(0, Damage);
                return Damage;
        }
    }
}
