using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace ThirteenIsh.Commands;

/// <summary>
/// Prints out the Thirteenish version and details, and exits.
/// </summary>
internal sealed class AboutCommand : CommandBase
{
    private static AboutDetails? _details;

    public AboutCommand() : base("about", "Prints out information about Thirteenish")
    {
    }

    public override bool IsGlobal => true;

    public override Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var details = LazyInitializer.EnsureInitialized(ref _details, BuildAboutDetails);

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Thirteenish")
            .WithDescription("A roleplaying bot")
            .AddField("Version", details.Version);

        return command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
    }

    private AboutDetails BuildAboutDetails()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var versionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?? throw new InvalidOperationException("No version attribute found");

        return new AboutDetails(versionAttr.InformationalVersion);
    }

    private record AboutDetails(string Version);
}
