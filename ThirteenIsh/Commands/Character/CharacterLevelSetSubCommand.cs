using Discord;
using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Character;

internal sealed class CharacterLevelSetSubCommand() : SubCommandBase("set", "Sets a character's level.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddCharacterOption()
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("value")
                .WithDescription("The new character level.")
                .WithMaxValue(10)
                .WithMinValue(1)
                .WithType(ApplicationCommandOptionType.Integer)
                );
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "character", out var characterName))
        {
            await command.RespondAsync("Character not found", ephemeral: true);
            return;
        }

        if (!CommandUtil.TryGetOption<int>(option, "value", out var level))
        {
            await command.RespondAsync("No value provided", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.UpdateCharacterAsync(characterName, UpdateSheet, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync($"Cannot edit a character named '{characterName}'. Perhaps they do not exist?",
                ephemeral: true);
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"Edited level for {characterName}");
        embedBuilder.WithDescription($"{character.Sheet.Level}");

        await command.RespondAsync(embed: embedBuilder.Build());

        void UpdateSheet(CharacterSheet sheet)
        {
            sheet.Level = level;
        }
    }
}
