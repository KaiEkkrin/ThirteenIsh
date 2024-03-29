using System.Text;

namespace ThirteenIsh.Game;

internal readonly record struct TableCell(string Text, bool RightJustify = false)
{
    public static readonly TableCell Empty = new(string.Empty);

    public static TableCell Integer(int value) => new($"{value}", true);
}

/// <summary>
/// Base class for the possible table rows for the TableHelper.
/// </summary>
internal abstract class TableRowBase
{
    public abstract void Append(StringBuilder builder, int[] maxCellSizes, char paddingCharacter);

    public abstract void ContributeMaxCellSizes(int[] maxCellSizes);

    protected static void AppendJustified(StringBuilder builder, int maxCellWidth, string text, bool rightJustify,
        char paddingCharacter)
    {
        // Value if left justify
        if (!rightJustify) builder.Append(text);

        // Padding
        var paddingLength = maxCellWidth - text.Length;
        for (var j = 0; j < paddingLength; ++j) builder.Append(paddingCharacter);

        // Value if right justify
        if (rightJustify) builder.Append(text);
    }
}

/// <summary>
/// A standard table row with multiple columns.
/// </summary>
internal sealed class TableRow(params TableCell[] cells) : TableRowBase
{
    public override void Append(StringBuilder builder, int[] maxCellSizes, char paddingCharacter)
    {
        for (var i = 0; i < cells.Length; ++i)
        {
            var cell = cells[i];

            // Between-cell padding
            if (i > 0)
            {
                for (var j = 0; j < TableHelper.CellPaddingLength; ++j) builder.Append(paddingCharacter);
            }

            // Cell text
            AppendJustified(builder, maxCellSizes[i], cell.Text, cell.RightJustify, paddingCharacter);
        }
    }

    public override void ContributeMaxCellSizes(int[] maxCellSizes)
    {
        if (cells.Length != maxCellSizes.Length)
            throw new InvalidOperationException(
                $"A row with {cells.Length} cells does not fit into a table of width {maxCellSizes.Length}");

        for (var i = 0; i < cells.Length; ++i)
        {
            maxCellSizes[i] = Math.Max(maxCellSizes[i], cells[i].Text.Length);
        }
    }
}

/// <summary>
/// A table row that spans the whole table with a single text.
/// Does not contribute to the calculation for the table width but rather, truncates
/// the text to fit as required.
/// </summary>
internal sealed class SpanningTableRow(string text, bool rightJustify = false) : TableRowBase
{
    public override void Append(StringBuilder builder, int[] maxCellSizes, char paddingCharacter)
    {
        var maxWidth = maxCellSizes.Sum() + TableHelper.CellPaddingLength * (maxCellSizes.Length - 1);
        if (text.Length >= maxWidth)
        {
            // Fill the whole row with truncated text
            builder.Append(text[..maxWidth]);
            return;
        }

        // Otherwise, justify the text within the row
        AppendJustified(builder, maxWidth, text, rightJustify, paddingCharacter);
    }

    public override void ContributeMaxCellSizes(int[] maxCellSizes)
    {
    }
}
