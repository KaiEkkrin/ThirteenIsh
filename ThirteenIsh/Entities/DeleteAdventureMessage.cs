﻿using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities;

public class DeleteAdventureMessage : MessageBase
{
    /// <summary>
    /// The guild ID.
    /// </summary>
    public DiscordId GuildId { get; set; } = new();

    /// <summary>
    /// The adventure name to delete.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public override async Task HandleAsync(SocketMessageComponent component, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();
        var updatedGuild = await dataService.EditGuildAsync(
            new EditOperation(Name), GuildId.Value, cancellationToken);

        if (updatedGuild is null)
        {
            await component.RespondAsync(
                $"Cannot delete an adventure named '{Name}'. Perhaps it was already deleted?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(component.User);
        embedBuilder.WithTitle($"Deleted adventure: {Name}");

        await component.RespondAsync(embed: embedBuilder.Build());
    }

    private sealed class EditOperation(string adventureName) : SyncEditOperation<Guild, Guild, EditResult<Guild>>
    {
        public override EditResult<Guild> DoEdit(Guild guild)
        {
            if (!guild.Adventures.Any(o => o.Name == adventureName))
                return new EditResult<Guild>(null); // adventure does not exist

            guild.Adventures.RemoveAll(o => o.Name == adventureName);
            if (guild.CurrentAdventureName == adventureName) guild.CurrentAdventureName = string.Empty;
            return new EditResult<Guild>(guild);
        }
    }
}