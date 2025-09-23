namespace ThirteenIsh.Game;

/// <summary>
/// Exception thrown when editing a character property fails.
/// </summary>
public sealed class EditCharacterPropertyException : Exception
{
    public EditCharacterPropertyException()
    {
    }

    public EditCharacterPropertyException(string? message) : base(message)
    {
    }

    public EditCharacterPropertyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}