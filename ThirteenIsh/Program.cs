using ThirteenIsh;
using ThirteenIsh.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddCommandLine(args)
    .AddEnvironmentVariables()
    .AddUserSecrets<Worker>();

CommandRegistration.RegisterCommands(builder.Services);
builder.Services
    .AddSingleton<DataService>()
    .AddSingleton<DiscordService>()
    .AddHostedService<Worker>();

var host = builder.Build();
host.Run();
