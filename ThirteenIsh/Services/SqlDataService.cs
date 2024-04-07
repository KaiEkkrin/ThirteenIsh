using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System.Diagnostics;
using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.Services;

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

    public async Task<Character?> DeleteCharacterAsync(string name, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
    {
        var character = await GetCharacterAsync(name, userId, characterType, cancellationToken);
        if (character is null) return null;

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync(cancellationToken);
        return character;
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

    public async Task<Character?> GetCharacterAsync(string name, ulong userId, CharacterType characterType,
        CancellationToken cancellationToken = default)
    {
        // Only accept an exact match
        var matchingCharacters = await _context.Characters
            .Where(c => c.UserId == userId && c.CharacterType == characterType &&
                        c.NameUpper.StartsWith(name.ToUpperInvariant()))
            .Take(2)
            .ToListAsync(cancellationToken);

        return matchingCharacters.Count == 1 ? matchingCharacters[0] : null;
    }
}
