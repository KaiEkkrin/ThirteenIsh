using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Commands;
using ThirteenIsh.Game;
using ThirteenIsh.Services;
using CharacterType = ThirteenIsh.Database.Entities.CharacterType;

namespace ThirteenIsh.Entities.Messages;

public class ResetAdventurerMessage : MessageBase
{
    /// <summary>
    /// The guild ID.
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    /// <summary>
    /// The adventure name to reset this user's character in.
    /// </summary>
    public string AdventureName { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (adventure, message) = await dataService.EditGuildAsync(
            new EditOperation(AdventureName, NativeUserId), NativeGuildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await component.RespondAsync(message, ephemeral: true);
            return true;
        }

        if (adventure is null) throw new InvalidOperationException(nameof(adventure));

        var gameSystem = GameSystem.Get(adventure.GameSystem);
        var adventurer = adventure.Adventurers[component.User.Id];

        await CommandUtil.RespondWithAdventurerSummaryAsync(component, adventurer, gameSystem,
            new CommandUtil.AdventurerSummaryOptions
            {
                OnlyVariables = true,
                Title = $"Reset adventurer {AdventureName}"
            });

        return true;
    }

    private sealed class EditOperation(string adventureName, ulong userId)
        : SyncEditOperation<ResultOrMessage<Adventure>, Guild, MessageEditResult<Adventure>>
    {
        public override MessageEditResult<Adventure> DoEdit(Guild guild)
        {
            var adventure = guild.Adventures.FirstOrDefault(o => o.Name == adventureName);
            if (adventure is null)
                return new MessageEditResult<Adventure>(null, $"Cannot find an adventure named '{adventureName}'.");

            if (!adventure.Adventurers.TryGetValue(userId, out var adventurer))
                return new MessageEditResult<Adventure>(null, $"You do not have a character in the adventure '{adventureName}'.");

            var characterSystem = GameSystem.Get(adventure.GameSystem).GetCharacterSystem(CharacterType.PlayerCharacter);
            characterSystem.ResetVariables(adventurer);

            return new MessageEditResult<Adventure>(adventure);
        }
    }
}
