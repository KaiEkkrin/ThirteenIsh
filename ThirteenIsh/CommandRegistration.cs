using System.Reflection;
using ThirteenIsh.Commands;

namespace ThirteenIsh;

internal static class CommandRegistration
{
    private static readonly List<Type> Commands = [];

    public static IEnumerable<Type> AllCommands => Commands;

    public static void RegisterCommands(IServiceCollection services)
    {
        Commands.Clear();
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!ty.IsClass || ty.IsAbstract || !ty.IsAssignableTo(typeof(CommandBase))) continue;

            services.AddSingleton(ty);
            Commands.Add(ty);
        }
    }
}
