using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Entities.Messages;

public class EncounterDamageMessage : MessageBase
{
    /// <summary>
    /// The name of the character taking damage.
    /// (TODO Support monster damage.)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The amount of damage.
    /// </summary>
    public int Damage { get; set; }

    public override Task<bool> HandleAsync(SocketMessageComponent component, string controlId,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dataService = serviceProvider.GetRequiredService<DataService>();

        // TODO support reducing or eliminating this damage optionally via the message component buttons.
        throw new NotImplementedException();
    }
}
