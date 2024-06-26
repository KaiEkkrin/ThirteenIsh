using Discord;
using Discord.WebSocket;
using ThirteenIsh.Game;
using ThirteenIsh.Services;

namespace ThirteenIsh.Commands.Combat;

internal sealed class CombatGetSubCommand(bool asGm)
    : SubCommandBase("get", "Shows the details of a combatant.")
{
    public override SlashCommandOptionBuilder CreateBuilder()
    {
        return base.CreateBuilder()
            .AddOption("alias", ApplicationCommandOptionType.String, "The combatant alias to get.",
                isRequired: asGm)
            .AddOption("full", ApplicationCommandOptionType.Boolean, "Include full character sheet");
    }

    public override async Task HandleAsync(SocketSlashCommand command, SocketSlashCommandDataOption option,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (command is not { ChannelId: { } channelId, GuildId: { } guildId }) return;

        var alias = CommandUtil.TryGetOption<string>(option, "alias", out var aliasString)
            ? aliasString
            : null;

        if (asGm && string.IsNullOrEmpty(alias))
        {
            await command.RespondAsync($"A valid alias must be supplied.", ephemeral: true);
            return;
        }

        var onlyVariables = !CommandUtil.TryGetOption<bool>(option, "full", out var full) || !full;



        var dataService = serviceProvider.GetRequiredService<SqlDataService>();
        var guild = await dataService.GetGuildAsync(guildId, cancellationToken);
        var result = await dataService.GetCombatantResultAsync(guild, channelId, asGm ? null : command.User.Id,
            alias, cancellationToken);

        await result.Handle(
            errorMessage => command.RespondAsync(errorMessage, ephemeral: true),
            output =>
            {
                var (adventure, _, _, character) = output;
                var gameSystem = GameSystem.Get(adventure.GameSystem);
                return CommandUtil.RespondWithTrackedCharacterSummaryAsync(command, character, gameSystem,
                    new CommandUtil.AdventurerSummaryOptions
                    {
                        Flags = onlyVariables
                            ? CommandUtil.AdventurerSummaryFlags.OnlyVariables
                            : CommandUtil.AdventurerSummaryFlags.WithTags,
                        Title = character.Name
                    },
                    asGm);
            });
    }
}
