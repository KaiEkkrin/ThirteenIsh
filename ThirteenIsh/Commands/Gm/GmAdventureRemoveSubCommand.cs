using Discord;
using Discord.WebSocket;
using ThirteenIsh.Database.Entities.Messages;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Gm;

internal sealed class GmAdventureRemoveSubCommand() : SubCommandBase("remove", "Deletes an adventure.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("name", ApplicationCommandOptionType.String, "The adventure name.",
                isRequired: true);
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command.GuildId is not { } guildId) return;
        if (!CommandUtil.TryGetCanonicalizedMultiPartOption(option, "name", out var name))
        {
            await command.RespondAsync("Adventure names must contain only letters and spaces", ephemeral: true);
            return;
        }

        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var adventure = await dataService.GetAdventureAsync(guild, name, cancellationToken);
        if (adventure is null)
        {
            await command.RespondAsync($"Cannot find an adventure named '{name}'. Perhaps it was already deleted?");
            return;
        }

        // I'm not going to delete this right away but instead give the user a confirm button
        DeleteAdventureMessage message = new()
        {
            GuildId = guildId,
            Name = name,
            UserId = command.User.Id
        };
        await dataService.AddMessageAsync(message, cancellationToken);

        ComponentBuilder builder = new();
        builder.WithButton("Delete", message.GetMessageId(), ButtonStyle.Danger);

        await command.RespondAsync($"Do you really want to delete the adventure named '{name}'? This cannot be undone.",
            ephemeral: true, components: builder.Build());
    }
}
