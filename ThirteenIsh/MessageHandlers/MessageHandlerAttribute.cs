namespace ThirteenIsh.MessageHandlers;

/// <summary>
/// Stick this attribute on message handlers so the registration code associates them with
/// the correct message type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MessageHandlerAttribute : Attribute
{
    public required Type MessageType { get; init; }
}
