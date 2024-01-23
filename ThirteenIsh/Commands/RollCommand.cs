using Discord;
using Discord.WebSocket;

namespace ThirteenIsh.Commands;

internal sealed class RollCommand : CommandBase
{
    public RollCommand() : base("roll", "Makes basic dice rolls")
    {
    }

    public override SlashCommandBuilder CreateBuilder()
    {
        var builder = base.CreateBuilder();
        builder.AddOption("dice", ApplicationCommandOptionType.String, "The dice expression to evaluate",
            isRequired: true);

        return builder;
    }

    public override Task HandleAsync(SocketSlashCommand command)
    {
        var diceString = command.Data.Options.Where(o => o.Name == "dice")
            .Select(o => o.Value.ToString())
            .First();

        return command.RespondAsync($"TODO Evaluating dice command: {diceString}");
    }
}
