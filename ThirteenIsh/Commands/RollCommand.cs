using Discord;
using Discord.WebSocket;
using ThirteenIsh.Parsing;

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
            .First() ?? string.Empty;

        diceString = diceString.Trim();
        var parseTree = MultiCaseParser.DiceRollOrIntegerParser.Parse(diceString, 0);
        if (!string.IsNullOrEmpty(parseTree.Error))
            return command.RespondAsync(parseTree.Error);

        if (parseTree.Offset < diceString.Length)
            return command.RespondAsync($"Unrecognised input at end of string: '{diceString[parseTree.Offset..]}'");

        var value = parseTree.Evaluate(out var working);

        EmbedBuilder embedBuilder = new();
        embedBuilder.WithAuthor(command.User);
        embedBuilder.WithTitle($"rolled {value}");
        embedBuilder.WithDescription(working);
        embedBuilder.WithCurrentTimestamp();

        return command.RespondAsync(embed: embedBuilder.Build());
    }
}
