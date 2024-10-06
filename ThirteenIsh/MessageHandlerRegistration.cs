using System.Collections.Frozen;
using System.Reflection;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.MessageHandlers;

namespace ThirteenIsh;

internal static class MessageHandlerRegistration
{
    // Maps message type -> handler type
    private static FrozenDictionary<Type, Type>? MessageHandlers;

    public static void RegisterMessageHandlers(IServiceCollection services)
    {
        Dictionary<Type, Type> handlers = [];
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!ty.IsClass || ty.IsAbstract || !ty.IsAssignableTo(typeof(IMessageHandler)) ||
                ty.GetCustomAttribute<MessageHandlerAttribute>() is not { } attribute) continue;

            services.AddTransient(ty);
            if (!handlers.TryAdd(attribute.MessageType, ty))
                throw new InvalidOperationException(
                    $"{ty} is a message handler for {attribute.MessageType} but one is already registered for that type");
        }

        MessageHandlers = handlers.ToFrozenDictionary();
    }

    public static IMessageHandler ResolveMessageHandler(this IServiceProvider serviceProvider, MessageBase message)
    {
        if (MessageHandlers is null) throw new InvalidOperationException("Message handlers not registered");
        if (!MessageHandlers.TryGetValue(message.GetType(), out var handlerType))
            throw new InvalidOperationException($"No handler type registered for message type {message.GetType()}");

        return (IMessageHandler)serviceProvider.GetRequiredService(handlerType);
    }
}
