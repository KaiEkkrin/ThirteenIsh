using ThirteenIsh;
using ThirteenIsh.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddCommandLine(args)
    .AddEnvironmentVariables()
    .AddUserSecrets<Worker>();

builder.Services.AddSingleton<DiscordService>()
    .AddHostedService<Worker>();

var host = builder.Build();
host.Run();
