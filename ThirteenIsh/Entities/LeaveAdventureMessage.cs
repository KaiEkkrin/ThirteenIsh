using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Commands;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities;

public class LeaveAdventureMessage : MessageBase
{
    /// <summary>
    /// The guild ID.
    /// </summary>
    public long GuildId { get; set; }

    [BsonIgnore]
    public ulong NativeGuildId => (ulong)GuildId;

    /// <summary>
    /// The adventure name to leave.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override async Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var (output, message) = await dataService.EditGuildAsync(
            new EditOperation(Name, NativeUserId), NativeGuildId, cancellationToken);

        if (!string.IsNullOrEmpty(message))
        {
            await component.RespondAsync(message, ephemeral: true);
            return true;
        }

        if (output is null) throw new InvalidOperationException(nameof(output));

        var discordService = serviceProvider.GetRequiredService<DiscordService>();
        await discordService.RespondWithAdventureSummaryAsync(component, output.Guild, output.Adventure,
            $"Left adventure {Name}");

        return true;
    }

    private sealed class EditOperation(string adventureName, ulong userId)
        : SyncEditOperation<ResultOrMessage<EditOutput>, Guild, MessageEditResult<EditOutput>>
    {
        public override MessageEditResult<EditOutput> DoEdit(Guild guild)
        {
            var adventure = guild.Adventures.FirstOrDefault(o => o.Name == adventureName);
            if (adventure is null)
                return new MessageEditResult<EditOutput>(null, $"Cannot find an adventure named '{adventureName}'.");

            var left = adventure.Adventurers.Remove(userId);
            if (!left)
                return new MessageEditResult<EditOutput>(null, $"You do not have a character in the adventure '{adventureName}'.");

            return new MessageEditResult<EditOutput>(new EditOutput(guild, adventure));
        }
    }

    private sealed record EditOutput(Guild Guild, Adventure Adventure);
}
