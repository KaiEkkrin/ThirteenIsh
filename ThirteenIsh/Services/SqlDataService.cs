using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Diagnostics;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Results;

namespace ThirteenIsh.Services;

// This warning is about specifying a StringComparison enum on string.StartsWith -- we need to pass the
// one-argument overload to EF Linq in order for it to successfully translate to SQL statements
#pragma warning disable CA1862

/// <summary>
/// SQL database adapter
/// </summary>
public sealed class SqlDataService(DataContext context, ILogger<SqlDataService> logger)
{
    private static readonly Action<ILogger, long, TimeSpan, Exception?> DeletedOldMessages =
        LoggerMessage.Define<long, TimeSpan>(
            LogLevel.Information,
            new EventId(1, nameof(SqlDataService)),
            "Deleted {Count} old messages in {Elapsed}");

    private static readonly Action<ILogger, string, Exception> ErrorDeletingOldMessages =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(SqlDataService)),
            "Error deleting old messages: {Message}");

    private readonly DataContext _context = context;
    private readonly ILogger<SqlDataService> _logger = logger;

    // Retry policy
    private static readonly IEnumerable<TimeSpan> Delays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(5), 6);

    public async Task<ResultOrMessage<AdventureResult>> AddAdventureAsync(ulong guildId, string name, string description,
        string gameSystem, CancellationToken cancellationToken = default)
    {
        // TODO find out what error to handle when the adventure already exists.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        guild.CurrentAdventureName = name;
        Adventure adventure = new()
        {
            GuildId = guild.Id,
            Name = name,
            Description = description,
            GameSystem = gameSystem
        };

        _context.Adventures.Add(adventure);
        await _context.SaveChangesAsync(cancellationToken);
        return new ResultOrMessage<AdventureResult>(new AdventureResult(guild, adventure));
    }

    public async Task<ResultOrMessage<EncounterResult>> AddEncounterAsync(
        ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        // TODO find out what error to handle when the encounter already exists due to conflict...
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter is not null) return new ResultOrMessage<EncounterResult>(
            null, "There is already an active encounter in this channel.");

        var adventure = await GetAdventureAsync(guild, guild.CurrentAdventureName, cancellationToken);
        if (adventure == null) return new ResultOrMessage<EncounterResult>(
            null, "There is no current adventure.");

        encounter = new Encounter()
        {
            AdventureName = adventure.Name,
            ChannelId = channelId,
            GuildId = guild.Id,
            Round = 1
        };
        _context.Encounters.Add(encounter);
        await _context.SaveChangesAsync(cancellationToken);
        return new ResultOrMessage<EncounterResult>(new EncounterResult(adventure, encounter));
    }

    public async Task AddMessageAsync(MessageBase message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Character?> CreateCharacterAsync(string name, CharacterType characterType,
        string gameSystem, ulong userId, CancellationToken cancellationToken = default)
    {
        // TODO what error happens when the character already exists / their name overlaps
        // another one?
        Character character = new()
        {
            Name = name,
            CharacterType = characterType,
            GameSystem = gameSystem,
            UserId = userId
        };

        _context.Characters.Add(character);
        await _context.SaveChangesAsync(cancellationToken);
        return character;
    }

    public async Task<Adventure?> DeleteAdventureAsync(ulong guildId, string name,
        CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with the adventure being concurrently deleted.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var adventure = await GetAdventureAsync(guild, name, cancellationToken);
        if (adventure == null) return null;

        if (guild.CurrentAdventureName == adventure.Name) guild.CurrentAdventureName = string.Empty;
        _context.Adventures.Remove(adventure);
        await _context.SaveChangesAsync(cancellationToken);
        return adventure;
    }

    public async Task<Adventurer?> DeleteAdventurerAsync(ulong guildId, ulong userId, string adventureName,
        CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var adventure = await GetAdventureAsync(guild, adventureName, cancellationToken);
        if (adventure == null) return null;

        var adventurer = await GetAdventurerAsync(adventure, userId, cancellationToken);
        if (adventurer == null) return null;

        _context.Adventurers.Remove(adventurer);
        await _context.SaveChangesAsync(cancellationToken);
        return adventurer;
    }

    public async Task<Character?> DeleteCharacterAsync(string name, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
    {
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken: cancellationToken);
        if (character is null) return null;

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync(cancellationToken);
        return character;
    }

    public async Task<Encounter?> DeleteEncounterAsync(ulong guildId, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter == null) return null;

        _context.Encounters.Remove(encounter);
        await _context.SaveChangesAsync(cancellationToken);
        return encounter;
    }

    public async Task DeleteMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        await _context.Messages.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteOldMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var expiredTime = DateTimeOffset.UtcNow - MessageBase.Timeout;
            var count = await _context.Messages.Where(m => m.Timestamp < expiredTime).ExecuteDeleteAsync(cancellationToken);

            stopwatch.Stop();
            DeletedOldMessages(_logger, count, stopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            ErrorDeletingOldMessages(_logger, ex.Message, ex);
        }
    }

    /// <summary>
    /// Call this to write changes to the database, within a retry loop that reloads entities
    /// and reruns the operation if there is a conflict.
    /// TODO With this pattern, the operation should always be synchronous -- correct?
    /// </summary>
    public async Task<T?> EditAsync<T, TParam, TResult>(
        EditOperation<T, TParam, TResult> operation,
        TParam parameter,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        var retryPolicy = Policy.Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(Delays, (exception, _) => OnRetryAsync(exception, cancellationToken));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var result = await operation.DoEditAsync(_context, parameter, cancellationToken);
            if (!result.Success) return result.Value;

            await _context.SaveChangesAsync(cancellationToken);
            return result.Value;
        });
    }

    public async Task<T?> EditAdventureAsync<T, TResult>(
        ulong guildId, EditOperation<T, Adventure, TResult> operation,
        string? adventureName = null,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        adventureName ??= guild.CurrentAdventureName;

        var adventure = await GetAdventureAsync(guild, adventureName, cancellationToken);
        if (adventure == null) return operation.CreateError($"Adventure '{adventureName}' not found.").Value;

        return await EditAsync(operation, adventure, cancellationToken);
    }

    public async Task<T?> EditCharacterAsync<T, TResult>(
        string name, EditOperation<T, Character, TResult> operation, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken: cancellationToken);
        if (character == null) return operation.CreateError($"Character '{name}' not found.").Value;

        return await EditAsync(operation, character, cancellationToken);
    }

    public async Task<T?> EditEncounterAsync<T, TResult>(
        ulong guildId, ulong channelId, EditOperation<T, EncounterResult, TResult> operation,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var result = await GetEncounterResultAsync(guild, channelId, cancellationToken);
        if (!string.IsNullOrEmpty(result.Value.ErrorMessage))
            return operation.CreateError(result.Value.ErrorMessage).Value;

        if (result.Value.Value == null)
            throw new InvalidOperationException("GetEncounterResultAsync did not return a value");

        return await EditAsync(operation, result.Value.Value, cancellationToken);
    }

    // TODO I should possibly extend this with helpers that fish up current adventurer(s), encounter(s), etc...?
    public async Task<T?> EditGuildAsync<T, TResult>(EditOperation<T, Guild, TResult> operation, ulong guildId,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        // TODO can I use EditResult as the basis for one method that deals with transactions, refreshing
        // all entities with the database versions and re-attempting the operation on conflict?
        var guild = await GetGuildAsync(guildId, cancellationToken);
        if (guild == null) return operation.CreateError($"Guild '{guildId}' cannot be created for some reason?").Value;

        return await EditAsync(operation, guild, cancellationToken);
    }

    public async Task<Guild> EnsureGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await _context.Guilds.SingleOrDefaultAsync(g => g.GuildId == guildId, cancellationToken);
        if (guild is null)
        {
            guild = new Guild { GuildId = guildId };
            _context.Guilds.Add(guild);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return guild;
    }

    public async Task<Adventure?> GetAdventureAsync(Guild guild, string? name,
        CancellationToken cancellationToken = default)
    {
        name ??= guild.CurrentAdventureName;

        // Look for an exact match first
        var adventure = await _context.Adventures
            .SingleOrDefaultAsync(c => c.GuildId == guild.Id && c.Name == name, cancellationToken);

        if (adventure != null || name == null) return adventure;

        // Only accept an unambiguous match
        var matchingAdventures = await _context.Adventures
            .Where(a => a.GuildId == guild.Id && a.NameUpper.StartsWith(name.ToUpperInvariant()))
            .OrderBy(a => a.Name)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingAdventures.Count == 1 ? matchingAdventures[0] : null;
    }

    public Task<Adventurer?> GetAdventurerAsync(Adventure adventure, ulong userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Adventurers.SingleOrDefaultAsync(a => a.AdventureId == adventure.Id && a.UserId == userId,
            cancellationToken);
    }

    public IAsyncEnumerable<Adventure> GetAdventuresAsync(Guild guild)
    {
        return _context.Adventures.Where(a => a.GuildId == guild.Id).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Adventurer> GetAdventurersAsync(Adventure adventure)
    {
        return _context.Adventurers.Where(a => a.AdventureId == adventure.Id)
            .AsAsyncEnumerable();
    }

    public async Task<Character?> GetCharacterAsync(string name, ulong userId, CharacterType characterType,
        bool asTracking = true, CancellationToken cancellationToken = default)
    {
        // Look for an exact match first
        var character = await _context.Characters
            .AsTracking(asTracking ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
            .SingleOrDefaultAsync(c => c.UserId == userId && c.CharacterType == characterType && c.Name == name,
                cancellationToken);

        if (character != null) return character;

        // Only accept an unambiguous match
        var matchingCharacters = await ListCharactersAsync(name, userId, characterType, asTracking)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingCharacters.Count == 1 ? matchingCharacters[0] : null;
    }

    public Task<ITrackedCharacter?> GetCharacterAsync(CombatantBase combatant,
        CancellationToken cancellationToken = default)
    {
        return combatant.GetCharacterAsync(_context, cancellationToken);
    }

    public Task<Encounter?> GetEncounterAsync(Guild guild, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        // I'll always include the combatants here, because pretty much all operations want
        // to see them.
        return _context.Encounters
            .Include(e => e.Combatants)
            .SingleOrDefaultAsync(e => e.GuildId == guild.Id && e.ChannelId == channelId, cancellationToken);
    }

    public async Task<MessageEditResult<EncounterResult>> GetEncounterResultAsync(Guild guild, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter is null) return new MessageEditResult<EncounterResult>(null,
            "There is no active encounter in this channel.");

        if (encounter.AdventureName != guild.CurrentAdventureName) return new MessageEditResult<EncounterResult>(null,
            $"The current encounter is in adventure '{encounter.AdventureName}', which is not the current adventure.");

        var adventure = await GetAdventureAsync(guild, encounter.AdventureName, cancellationToken);
        if (adventure is null) return new MessageEditResult<EncounterResult>(null,
            $"The current encounter is in adventure '{encounter.AdventureName}', which was not found.");

        return new MessageEditResult<EncounterResult>(new EncounterResult(adventure, encounter));
    }

    public async Task<Guild> GetGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        return await _context.Guilds.SingleOrDefaultAsync(g => g.GuildId == guildId, cancellationToken)
            ?? throw new InvalidOperationException($"No guild record found for {guildId}");
    }

    public Task<MessageBase?> GetMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Messages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public IAsyncEnumerable<Character> ListCharactersAsync(ulong userId, CharacterType characterType,
        bool asTracking = true)
    {
        return _context.Characters
            .AsTracking(asTracking ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
            .Where(c => c.UserId == userId && c.CharacterType == characterType)
            .AsAsyncEnumerable();
    }

    private IAsyncEnumerable<Character> ListCharactersAsync(string name, ulong userId, CharacterType characterType,
        bool asTracking)
    {
        return _context.Characters
            .AsTracking(asTracking ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
            .Where(c => c.UserId == userId && c.CharacterType == characterType &&
                        c.NameUpper.StartsWith(name.ToUpperInvariant()))
            .OrderBy(c => c.Name)
            .AsAsyncEnumerable();
    }

    public async Task UpdateGuildCommandVersionAsync(Guild guild, int commandVersion,
        CancellationToken cancellationToken = default)
    {
        // We really shouldn't encounter a concurrent modification of this one
        guild.CommandVersion = commandVersion;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static async Task OnRetryAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is not DbUpdateConcurrencyException updateException)
            throw new ArgumentException($"Unexpected exception type to OnRetryAsync : {exception.GetType()}", exception);

        // Reload all affected entries from database. The change should be retried
        foreach (var entry in updateException.Entries)
        {
            await entry.ReloadAsync(cancellationToken);
            if (entry.State == EntityState.Detached) throw exception; // can't handle this one for now
        }
    }
}

#pragma warning restore CA1862
