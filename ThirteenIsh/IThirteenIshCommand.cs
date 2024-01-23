using Discord.WebSocket;

namespace ThirteenIsh;

/// <summary>
/// Implement custom commands by implementing this and decorating the class
/// with [ThirteenthCommand].
/// </summary>
internal interface IThirteenIshCommand
{
    Task HandleAsync(SocketSlashCommand command);
}
