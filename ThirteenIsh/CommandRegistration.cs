using System.Reflection;

namespace ThirteenIsh;

internal static class CommandRegistration
{
    private static readonly List<(ThirteenIshCommandAttribute Attribute, Type CommandType)> Commands = [];

    public static IEnumerable<(ThirteenIshCommandAttribute Attribute, Type CommandType)> AllCommands => Commands;

    public static void RegisterCommands(IServiceCollection services)
    {
        Commands.Clear();
        foreach (var ty in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (ty.GetCustomAttribute<ThirteenIshCommandAttribute>() is not { } attribute) continue;

            services.AddScoped(ty);
            Commands.Add((attribute, ty));
        }
    }
}
