using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Diagnostics;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
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
    private readonly AsyncRetryPolicy _retryPolicy = Policy.Handle<WriteConflictException>().WaitAndRetryAsync(Delays);

    public async Task<ResultOrMessage<Adventure>> AddAdventureAsync(ulong guildId, string name, string description,
        string gameSystem, CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        // TODO find out what error to handle when the adventure already exists.
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
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
        return new ResultOrMessage<Adventure>(adventure);
    }

    public async Task<ResultOrMessage<EncounterResult>> AddEncounterAsync(
        ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
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
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
        var adventure = await GetAdventureAsync(guild, name, cancellationToken);
        if (adventure == null) return null;

        if (guild.CurrentAdventureName == adventure.Name) guild.CurrentAdventureName = string.Empty;
        _context.Adventures.Remove(adventure);
        await _context.SaveChangesAsync(cancellationToken);
        return adventure;
    }

    public async Task<Character?> DeleteCharacterAsync(string name, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
    {
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken);
        if (character is null) return null;

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync(cancellationToken);
        return character;
    }

    public async Task<Encounter?> DeleteEncounterAsync(ulong guildId, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
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

            var expiredTime = DateTimeOffset.Now - MessageBase.Timeout;
            var count = await _context.Messages.Where(m => m.Timestamp < expiredTime).ExecuteDeleteAsync(cancellationToken);

            stopwatch.Stop();
            DeletedOldMessages(_logger, count, stopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            ErrorDeletingOldMessages(_logger, ex.Message, ex);
        }
    }

    public async Task<T?> EditAdventureAsync<T, TResult>(
        ulong guildId, string name, EditOperation<T, Adventure, TResult> operation,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
        var adventure = await GetAdventureAsync(guild, name, cancellationToken);
        if (adventure == null) return operation.CreateError($"Adventure '{name}' not found.").Value;

        var editResult = await operation.DoEditAsync(adventure, cancellationToken);
        if (!editResult.Success) return editResult.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return editResult.Value;
    }

    public async Task<T?> EditCharacterAsync<T, TResult>(
        string name, EditOperation<T, Character, TResult> operation, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken);
        if (character == null) return operation.CreateError($"Character '{name}' not found.").Value;

        var editResult = await operation.DoEditAsync(character, cancellationToken);
        if (!editResult.Success) return editResult.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return editResult.Value;
    }

    public async Task<T?> EditEncounterAsync<T, TResult>(
        ulong guildId, ulong channelId, EditOperation<T, EncounterResult, TResult> operation,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter is null) return operation.CreateError("There is no active encounter in this channel.").Value;

        var adventure = await GetAdventureAsync(guild, encounter.AdventureName, cancellationToken);
        if (adventure is null) return operation.CreateError(
            $"The current encounter is in adventure '{encounter.AdventureName}', which was not found.").Value;

        var editResult = await operation.DoEditAsync(new EncounterResult(adventure, encounter), cancellationToken);
        if (!editResult.Success) return editResult.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return editResult.Value;
    }

    // TODO I should possibly extend this with helpers that fish up current adventurer(s), encounter(s), etc...?
    public async Task<T?> EditGuildAsync<T, TResult>(EditOperation<T, Guild, TResult> operation, ulong guildId,
        CancellationToken cancellationToken = default)
        where TResult : EditResult<T>
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        // TODO can I use EditResult as the basis for one method that deals with transactions, refreshing
        // all entities with the database versions and re-attempting the operation on conflict?
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
        if (guild == null) return operation.CreateError($"Guild '{guildId}' cannot be created for some reason?").Value;

        var editResult = await operation.DoEditAsync(guild, cancellationToken);
        if (!editResult.Success) return editResult.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return editResult.Value;
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

    public async Task<Adventure?> GetAdventureAsync(Guild guild, string name,
        CancellationToken cancellationToken = default)
    {
        // Look for an exact match first
        var adventure = await _context.Adventures
            .SingleOrDefaultAsync(c => c.GuildId == guild.Id && c.Name == name, cancellationToken);

        if (adventure != null) return adventure;

        // Only accept an unambiguous match
        var matchingAdventures = await _context.Adventures
            .Where(a => a.GuildId == guild.Id && a.NameUpper.StartsWith(name.ToUpperInvariant()))
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingAdventures.Count == 1 ? matchingAdventures[0] : null;
    }

    public async Task<Character?> GetCharacterAsync(string name, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
    {
        // Look for an exact match first
        var character = await _context.Characters
            .SingleOrDefaultAsync(c => c.UserId == userId && c.CharacterType == characterType && c.Name == name,
                cancellationToken);

        if (character != null) return character;

        // Only accept an unambiguous match
        var matchingCharacters = await ListCharactersAsync(name, userId, characterType)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingCharacters.Count == 1 ? matchingCharacters[0] : null;
    }

    public Task<Encounter?> GetEncounterAsync(Guild guild, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        return _context.Encounters.SingleOrDefaultAsync(e => e.GuildId == guild.Id && e.ChannelId == channelId,
            cancellationToken);
    }

    public Task<MessageBase?> GetMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Messages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public IAsyncEnumerable<Character> ListCharactersAsync(
        string name, ulong userId, CharacterType characterType)
    {
        return _context.Characters
            .Where(c => c.UserId == userId && c.CharacterType == characterType &&
                        c.NameUpper.StartsWith(name.ToUpperInvariant()))
            .AsAsyncEnumerable();
    }

    public async Task UpdateGuildCommandVersionAsync(ulong guildId, int commandVersion,
        CancellationToken cancellationToken = default)
    {
        // TODO Find a sensible way to deal with optimistic concurrency using the retry policy.
        var guild = await EnsureGuildAsync(guildId, cancellationToken);
        guild.CommandVersion = commandVersion;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

#pragma warning restore CA1862
