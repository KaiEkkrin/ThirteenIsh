using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterListSubCommand(CharacterType characterType)
    : SubCommandBase("list", $"Lists your {characterType.FriendlyName(FriendlyNameOptions.Plural)}.")
{
    private const int DefaultPageSize = 10;

    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "List only characters beginning with this name or after")
            .AddOption("page-size", ApplicationCommandOptionType.Integer, "The page size", minValue: 1, maxValue: 25);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        CommandUtil.TryGetOption<string>(option, "name", out var name);
        if (!CommandUtil.TryGetOption<int>(option, "page-size", out var pageSize))
        {
            pageSize = DefaultPageSize;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new ListCharactersMessage
        {
            CharacterType = characterType,
            Name = name,
            PageSize = pageSize,
            UserId = command.User.Id
        });
    }
}
