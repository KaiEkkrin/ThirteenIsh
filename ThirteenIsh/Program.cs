using Microsoft.EntityFrameworkCore;
using ThirteenIsh.Database;
using ThirteenIsh.Services;

namespace ThirteenIsh;

// Our Program.cs must be this shape in order to be usable to EF Core design-time tools:
// https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli
public class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Worker>();
            })
            .ConfigureServices(context =>
            {
                CommandRegistration.RegisterCommands(context);

                context.AddDbContextPool<DataContext>((services, options) =>
                {
                    var configuration = services.GetRequiredService<IConfiguration>();
                    options.UseNpgsql(configuration[ConfigKeys.DbConnectionString]);
                });

                context
                    .AddSingleton<DataService>()
                    .AddSingleton<DiscordService>()
                    .AddSingleton<PinnedMessageService>()
                    .AddSingleton<IRandomWrapper, RandomWrapper>()
                    .AddHostedService<Worker>();
            });

        return builder;
    }

    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
}
