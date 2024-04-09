using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;
using ThirteenIsh.Services;

namespace ThirteenIsh.MessageHandlers;

[MessageHandler(MessageType = typeof(EncounterDamageMessage))]
internal sealed class EncounterDamageMessageHandler(SqlDataService dataService) : MessageHandlerBase<EncounterDamageMessage>
{
    protected override async Task<bool> HandleInternalAsync(SocketMessageComponent component, string controlId,
        EncounterDamageMessage message, CancellationToken cancellationToken = default)
    {
        var guild = await dataService.EnsureGuildAsync(message.GuildId, cancellationToken);
        var encounter = await dataService.GetEncounterAsync(guild, message.ChannelId, cancellationToken);
        if (encounter == null)
        {
            await component.RespondAsync("There is no encounter in progress in the designated channel.", ephemeral: true);
            return true;
        }

        var combatant = encounter.Combatants.SingleOrDefault(c => c.Alias == message.Alias);
        if (combatant == null)
        {
            await component.RespondAsync($"There is no combatant '{message.Alias}' in the current encounter.",
                ephemeral: true);
            return true;
        }

        var adventure = await dataService.GetAdventureAsync(guild, encounter.AdventureName, cancellationToken);
        if (adventure == null || adventure.Name != guild.CurrentAdventureName)
        {
            await component.RespondAsync("The current encounter does not match the current adventure.", ephemeral: true);
            return true;
        }

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var characterSystem = gameSystem.GetCharacterSystem(combatant.CharacterType);
        var counter = characterSystem.FindCounter(message.VariableName,
            c => c.Options.HasFlag(GameCounterOptions.HasVariable));
        if (counter == null)
        {
            await component.RespondAsync($"'{message.VariableName}' does not uniquely match a variable name.",
                ephemeral: true);
            return true;
        }

        // Illustrating this as a parse tree should make it clearer what has happened
        var totalDamage = GetDamageAndParseTree(controlId, message, out var parseTree);

        // TODO I need to complete this. Remember to deal with monsters as well as player characters,
        // which the old code didn't do
        throw new NotImplementedException();
    }

    private static int GetDamageAndParseTree(string controlId, EncounterDamageMessage message, out ParseTreeBase parseTree)
    {
        switch (controlId)
        {
            case EncounterDamageMessage.TakeHalfControlId:
                parseTree = new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, message.Damage),
                    new IntegerParseTree(0, 2),
                    '/');
                return message.Damage / 2;

            case EncounterDamageMessage.TakeNoneControlId:
                parseTree = new BinaryOperationParseTree(0,
                    new IntegerParseTree(0, message.Damage),
                    new IntegerParseTree(0, 0),
                    '*');
                return 0;

            default:
                parseTree = new IntegerParseTree(0, message.Damage);
                return message.Damage;
        }
    }
}
