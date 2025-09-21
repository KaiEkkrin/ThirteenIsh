using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Character;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterSetSubCommand(CharacterType characterType)
    : SubCommandBase("set", $"Sets a property for a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption("property-name", ApplicationCommandOptionType.String, "The property name to set.",
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.String, "The property value.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync(
                $"{characterType.FriendlyName(FriendlyNameOptions.CapitalizeFirstCharacter)} names must contain only letters and spaces", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "property-name", out var propertyName))
        {
            await command.RespondAsync("A property name is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<string>(option, "value", out var newValue))
        {
            await command.RespondAsync("A value is required.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CharacterSetMessage
        {
            CharacterType = characterType,
            Name = name,
            PropertyName = propertyName,
            NewValue = newValue,
            UserId = command.User.Id
        });
    }
}
