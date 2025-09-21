namespace ThirteenIsh.Game;

public class GamePropertyException : Exception
{
    public GamePropertyException()
    {
    }

    public GamePropertyException(string? message) : base(message)
    {
    }

    public GamePropertyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
