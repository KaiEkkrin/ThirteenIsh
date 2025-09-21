using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.ChannelMessages.Character;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

/// <summary>
/// Adds a custom counter to a character.
/// </summary>
internal sealed class CharacterCcAddSubCommand(CharacterType characterType)
    : SubCommandBase("add", $"Adds a custom counter to a {characterType.FriendlyName()}.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, $"The {characterType.FriendlyName()} name.",
                isRequired: true)
            .AddOption("cc-name", ApplicationCommandOptionType.String, "The custom counter name to add.",
                isRequired: true)
            .AddOption("value", ApplicationCommandOptionType.Integer, "The default value.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("The type of custom counter.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer)
                .AddChoice("Rollable", (int)GameCounterOptions.CanRoll)
                .AddChoice("Variable", (int)GameCounterOptions.HasVariable));
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

        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "cc-name", out var ccName))
        {
            await command.RespondAsync("A counter name is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var defaultValue))
        {
            await command.RespondAsync("A default value is required.", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "type", out var typeInt))
        {
            await command.RespondAsync("A type selection is required.", ephemeral: true);
            return;
        }

        var channelMessageService = serviceProvider.GetRequiredService<ChannelMessageService>();
        await channelMessageService.AddMessageAsync(command, new CharacterCcAddMessage
        {
            CharacterType = characterType,
            Name = name,
            CcName = ccName,
            DefaultValue = defaultValue,
            GameCounterOptions = (GameCounterOptions)typeInt,
            UserId = command.User.Id
        });
    }
}
