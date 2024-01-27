using Discord;
using Discord.WebSocket;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands;

internal sealed class ShowCharacterCommand : CommandBase
{
    public ShowCharacterCommand() : base("show-character", "Shows the details of a character")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("name", ApplicationCommandOptionType.String, "The character name",
            isRequired: true);

        return builder;
    }

    public override async Task HandleAsync(SocketSlashCommand command, IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetOption<string>(command.Data, "name", out var name))
        {
            await command.RespondAsync("Character not found");
            return;
        }

        var dataService = serviceProvider.GetRequiredService<DataService>();
        var character = await dataService.GetCharacterAsync(name, command.User.Id, cancellationToken);
        if (character is null)
        {
            await command.RespondAsync("Character not found");
            return;
        }

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle(character.Name);
        embedBuilder.WithDescription($"Level {character.Level} {character.Class}");

        foreach (var (abilityName, abilityScore) in character.AbilityScores)
        {
            embedBuilder.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName(abilityName)
                .WithValue(abilityScore));
        }

        await command.RespondAsync(embed: embedBuilder.Build());
    }
}
