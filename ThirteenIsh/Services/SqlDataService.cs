using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Results;

namespace ThirteenIsh.Services;

/// <summary>
/// SQL database adapter
/// </summary>
[SuppressMessage("Performance",
    "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons",
    Justification = "Required by EF Core Linq translator")]
public sealed partial class SqlDataService(DataContext context, ILogger<SqlDataService> logger)
{
    [LoggerMessage(Level = LogLevel.Information, EventId = 1, Message = "Deleted {Count} old messages in {Elapsed}")]
    private partial void DeletedOldMessages(int count, TimeSpan elapsed);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 2, Message = "Error deleting old messages: {Message}")]
    private partial void ErrorDeletingOldMessages(string message, Exception exception);

    private readonly DataContext _context = context;
    private readonly ILogger<SqlDataService> _logger = logger;

    // Retry policy
    private static readonly IEnumerable<TimeSpan> Delays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(5), 4);

    public async Task<AdventureResult> AddAdventureAsync(ulong guildId, string name, string description,
        string gameSystem, CancellationToken cancellationToken = default)
    {
        // TODO find out what error to handle when the adventure already exists.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        guild.CurrentAdventureName = name;
        Adventure adventure = new()
        {
            Guild = guild,
            Name = name,
            Description = description,
            GameSystem = gameSystem
        };

        _context.Adventures.Add(adventure);
        await _context.SaveChangesAsync(cancellationToken);
        return new AdventureResult(guild, adventure);
    }

    public async Task<EditResult<EncounterResult>> AddEncounterAsync(
        ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        // TODO find out what error to handle when the encounter already exists due to conflict...
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter is not null) return new EditResult<EncounterResult>(
            null, "There is already an active encounter in this channel.");

        var adventure = await GetAdventureAsync(guild, guild.CurrentAdventureName, cancellationToken);
        if (adventure == null) return new EditResult<EncounterResult>(
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
        return new EditResult<EncounterResult>(new EncounterResult(adventure, encounter));
    }

    public async Task AddMessageAsync(MessageBase message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Character?> CreateCharacterAsync(string name, CharacterType characterType,
        string gameSystem, ulong userId, Action<Character>? initialiseCharacter = null, CancellationToken cancellationToken = default)
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

        initialiseCharacter?.Invoke(character);
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
            DeletedOldMessages(count, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            ErrorDeletingOldMessages(ex.Message, ex);
        }
    }

    /// <summary>
    /// Call this to write changes to the database, within a retry loop that reloads entities
    /// and reruns the operation if there is a conflict.
    /// </summary>
    public async Task<EditResult<T>> EditAsync<T, TParam>(
        EditOperation<T, TParam> operation,
        TParam parameter,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var retryPolicy = Policy.Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(Delays, (exception, _) => OnRetryAsync(exception, cancellationToken));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var result = await operation.DoEditAsync(_context, parameter, cancellationToken);
            if (result.Success) await _context.SaveChangesAsync(cancellationToken);
            return result;
        });
    }

    public async Task<EditResult<T>> EditAdventureAsync<T>(
        ulong guildId, EditOperation<T, Adventure> operation,
        string? adventureName = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        adventureName ??= guild.CurrentAdventureName;

        var adventure = await GetAdventureAsync(guild, adventureName, cancellationToken);
        if (adventure == null) return operation.CreateError($"Adventure '{adventureName}' not found.");

        var result = await EditAsync(operation, adventure, cancellationToken);
        return result;
    }

    public async Task<EditResult<T>> EditAdventurerAsync<T>(
        ulong guildId, ulong userId, EditOperation<T, Adventurer> operation,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // This one always fetches the user's character in the current adventure (if any.)
        var guild = await GetGuildAsync(guildId, cancellationToken);
        if (string.IsNullOrEmpty(guild.CurrentAdventureName))
            return operation.CreateError("There is no current adventure in this guild.");

        var adventure = await GetAdventureAsync(guild, null, cancellationToken);
        if (adventure == null) return operation.CreateError($"Adventure '{guild.CurrentAdventureName}' not found.");

        var adventurer = await GetAdventurerAsync(adventure, userId, cancellationToken);
        if (adventurer == null) return operation.CreateError("You have not joined the current adventure.");

        var result = await EditAsync(operation, adventurer, cancellationToken);
        return result;
    }

    public async Task<EditResult<T>> EditAdventurerAsync<T>(
        ulong guildId, string name, EditOperation<T, Adventurer> operation,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // This one fetches the adventurer with the matching name, ignoring permissions.
        var guild = await GetGuildAsync(guildId, cancellationToken);
        if (string.IsNullOrEmpty(guild.CurrentAdventureName))
            return operation.CreateError("There is no current adventure in this guild.");

        var adventure = await GetAdventureAsync(guild, null, cancellationToken);
        if (adventure == null) return operation.CreateError($"Adventure '{guild.CurrentAdventureName}' not found.");

        var adventurer = await GetAdventurerAsync(adventure, name, cancellationToken);
        if (adventurer == null)
            return operation.CreateError($"There is no character named '{name}' in the current adventure.");

        var result = await EditAsync(operation, adventurer, cancellationToken);
        return result;
    }

    public async Task<EditResult<T>> EditCharacterAsync<T>(
        string name, EditOperation<T, Character> operation, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken: cancellationToken);
        if (character == null) return operation.CreateError($"Character '{name}' not found.");

        return await EditAsync(operation, character, cancellationToken);
    }

    public async Task<EditResult<T>> EditCombatantAsync<T>(
        ulong guildId, ulong channelId, ulong? userId, EditOperation<T, CombatantResult> operation,
        string? alias = null, CancellationToken cancellationToken = default)
        where T : class
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var result = await GetCombatantResultAsync(guild, channelId, userId, alias, cancellationToken);
        return await result.HandleAsync(
            errorMessage => operation.CreateError(errorMessage),
            value => EditAsync(operation, value, cancellationToken));
    }

    public async Task<EditResult<T>> EditEncounterAsync<T>(
        ulong guildId, ulong channelId, EditOperation<T, EncounterResult> operation,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var guild = await GetGuildAsync(guildId, cancellationToken);
        var result = await GetEncounterResultAsync(guild, channelId, cancellationToken);
        return await result.HandleAsync(
            errorMessage => operation.CreateError(errorMessage),
            value => EditAsync(operation, value, cancellationToken));
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
            .Include(c => c.Guild)
            .SingleOrDefaultAsync(c => c.GuildId == guild.Id && c.Name == name, cancellationToken);

        if (adventure != null || name == null) return adventure;

        // Only accept an unambiguous match
        var matchingAdventures = await _context.Adventures
            .Include(c => c.Guild)
            .Where(a => a.GuildId == guild.Id && a.NameUpper.StartsWith(name.ToUpperInvariant()))
            .OrderBy(a => a.Name)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingAdventures.Count == 1 ? matchingAdventures[0] : null;
    }

    public Task<Adventurer?> GetAdventurerAsync(Adventure adventure, ulong userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Adventurers
            .Include(a => a.Adventure)
            .Where(a => a.AdventureId == adventure.Id && a.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Adventurer?> GetAdventurerAsync(Adventure adventure, string name,
        CancellationToken cancellationToken = default)
    {
        // Look for an exact match first
        var adventurer = await _context.Adventurers
            .Include(a => a.Adventure)
            .SingleOrDefaultAsync(a => a.AdventureId == adventure.Id && a.Name == name,
                cancellationToken);

        if (adventurer != null) return adventurer;

        // Only accept an unambiguous match
        var matchingAdventurers = await _context.Adventurers
            .Include(a => a.Adventure)
            .Where(a => a.AdventureId == adventure.Id && 
                        a.NameUpper.StartsWith(name.ToUpperInvariant()))
            .ToListAsync(cancellationToken);

        return matchingAdventurers.Count == 1 ? matchingAdventurers[0] : null;
    }

    public IAsyncEnumerable<AdventureListResult> GetAdventuresAsync(Guild guild)
    {
        return _context.Adventures.AsNoTracking()
            .Where(a => a.GuildId == guild.Id)
            .OrderBy(a => a.Name)
            .Select(a => new AdventureListResult(a.Name, a.GameSystem, a.Adventurers.Count()))
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Adventurer> GetAdventurersAsync(Adventure adventure)
    {
        return _context.Adventurers.Where(a => a.AdventureId == adventure.Id)
            .OrderBy(a => a.Name)
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

    public Task<ITrackedCharacter?> GetCharacterAsync(CombatantBase combatant, Encounter encounter,
        CancellationToken cancellationToken = default)
    {
        return _context.GetCharacterAsync(combatant, encounter, cancellationToken);
    }

    public Task<List<Character>> GetCharactersPageAsync(ulong userId, CharacterType characterType,
        string? name, bool after, int pageSize, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Characters
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.CharacterType == characterType);

        if (!string.IsNullOrEmpty(name))
        {
            queryable = after
                ? queryable.Where(c => c.NameUpper.CompareTo(name.ToUpperInvariant()) > 0)
                : queryable.Where(c => c.NameUpper.CompareTo(name.ToUpperInvariant()) >= 0);
        }

        return queryable
            .OrderBy(c => c.NameUpper)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<EditResult<CombatantResult>> GetCombatantResultAsync(Guild guild, ulong channelId, ulong? userId,
        string? alias = null, CancellationToken cancellationToken = default)
    {
        var encounterResult = await GetEncounterResultAsync(guild, channelId, cancellationToken);
        return await encounterResult.HandleAsync(
            errorMessage => new EditResult<CombatantResult>(null, errorMessage),
            async value =>
            {
                var (adventure, encounter) = value;
                if (!TryGetCombatant(encounter, alias, userId, out var combatant, out var errorMessage))
                    return new EditResult<CombatantResult>(null, errorMessage);

                var character = await GetCharacterAsync(combatant, encounter, cancellationToken);
                if (character == null)
                    return new EditResult<CombatantResult>(null,
                        $"Cannot find a character sheet for the combatant with alias '{combatant.Alias}'.");

                return new EditResult<CombatantResult>(new CombatantResult(adventure, encounter, combatant, character));
            });
    }

    public Task<Encounter?> GetEncounterAsync(Guild guild, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        // I'll always include the combatants here, because pretty much all operations want
        // to see them.
        return _context.Encounters
            .SingleOrDefaultAsync(e => e.GuildId == guild.Id && e.ChannelId == channelId, cancellationToken);
    }

    public async Task<EditResult<EncounterResult>> GetEncounterResultAsync(Guild guild, ulong channelId,
        CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterAsync(guild, channelId, cancellationToken);
        if (encounter is null) return new EditResult<EncounterResult>(null,
            "There is no active encounter in this channel.");

        if (encounter.AdventureName != guild.CurrentAdventureName) return new EditResult<EncounterResult>(null,
            $"The current encounter is in adventure '{encounter.AdventureName}', which is not the current adventure.");

        var adventure = await GetAdventureAsync(guild, encounter.AdventureName, cancellationToken);
        if (adventure is null) return new EditResult<EncounterResult>(null,
            $"The current encounter is in adventure '{encounter.AdventureName}', which was not found.");

        return new EditResult<EncounterResult>(new EncounterResult(adventure, encounter));
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGuildCommandVersionAsync(Guild guild, int commandVersion,
        CancellationToken cancellationToken = default)
    {
        // We really shouldn't encounter a concurrent modification of this one
        guild.CommandVersion = commandVersion;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static bool TryGetCombatant(Encounter encounter, string? alias, ulong? userId,
        [MaybeNullWhen(false)] out CombatantBase combatant, [MaybeNullWhen(true)] out string errorMessage)
    {
        if (alias != null)
        {
            if (encounter.Combatants.FirstOrDefault(x => x.Alias == alias) is not { } matchingCombatant)
            {
                combatant = null;
                errorMessage = $"The alias '{alias}' does not match any combatant in the current encounter.";
                return false;
            }
            else if (!CanGetCombatant(matchingCombatant, userId))
            {
                combatant = null;
                errorMessage = $"The combatant with alias '{alias}' belongs to someone else.";
                return false;
            }
            else
            {
                combatant = matchingCombatant;
                errorMessage = null;
                return true;
            }
        }
        else if (encounter.Combatants.FirstOrDefault(x => x.CharacterType == CharacterType.PlayerCharacter &&
                                                          CanGetCombatant(x, userId)) is { } usersCombatant)
        {
            combatant = usersCombatant;
            errorMessage = null;
            return true;
        }
        else
        {
            combatant = null;
            errorMessage = "Specify an alias for the combatant to select.";
            return false;
        }
    }

    private static bool CanGetCombatant(CombatantBase combatant, ulong? userId)
    {
        return !userId.HasValue || combatant.UserId == userId.Value;
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
