namespace ThirteenIsh;

/// <summary>
/// Thrown when we detect a write conflict writing to the database.
/// </summary>
public class WriteConflictException : Exception
{
    public WriteConflictException()
    {
    }

    public WriteConflictException(string? message) : base(message)
    {
    }

    public WriteConflictException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
