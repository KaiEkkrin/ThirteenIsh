using Discord.WebSocket;
using ThirteenIsh.Entities;
using ThirteenIsh.Game;
using ThirteenIsh.Parsing;

namespace ThirteenIsh.Commands.Pcs;

internal class PcVModCommand() : PcVCommandBase("vmod", "Adds to or subtracts from a variable value,",
    "The variable name to change.", "A number or dice expression to change it by.")
{
    protected override VCommandEditOperation CreateEditOperation(SocketSlashCommand command,
        GameCounter counter, ParseTreeBase parseTree, IRandomWrapper random)
    {
        return new EditOperation(command, counter, parseTree, random);
    }

    private sealed class EditOperation(SocketSlashCommand command, GameCounter counter, ParseTreeBase parseTree,
        IRandomWrapper random)
        : VCommandEditOperation(command)
    {
        protected override MessageEditResult<VCommandResult> DoEditInternal(Adventure adventure, Adventurer adventurer)
        {
            var currentValue = counter.GetVariableValue(adventurer)
                ?? counter.GetStartingValue(adventurer.Sheet)
                ?? throw new InvalidOperationException($"Variable {counter.Name} has no current or starting value");

            var modParseTree = new BinaryOperationParseTree(0,
                new IntegerParseTree(0, currentValue, counter.Name),
                parseTree,
                '+');

            var newValue = modParseTree.Evaluate(random, out var working);
            counter.SetVariableClamped(newValue, adventurer);
            return new MessageEditResult<VCommandResult>(new VCommandResult(adventure, working));
        }
    }
}
