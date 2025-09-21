

namespace ThirteenIsh.Game;

public readonly record struct TableCell(string Text, bool RightJustify = false)
{
    public static readonly TableCell Empty = new(string.Empty);

    public static TableCell Integer(int value) => new($"{value}", true);
}

/// <summary>
/// A standard table row with multiple columns.
/// </summary>
public sealed class TableRow(params TableCell[] cells)
{
    public int CellCount => cells.Length;

    public int Append(StringBuilder builder, int[] maxCellSizes, char paddingCharacter)
    {
        var charactersWritten = 0;
        for (var i = 0; i < maxCellSizes.Length; ++i)
        {
            // Between-cell padding
            if (i > 0)
            {
                for (var j = 0; j < TableHelper.CellPaddingLength; ++j) builder.Append(paddingCharacter);
                charactersWritten += TableHelper.CellPaddingLength;
            }

            // Cell text
            charactersWritten += AppendJustified(
                builder, maxCellSizes[i], i < cells.Length ? cells[i].Text : string.Empty,
                i < cells.Length && cells[i].RightJustify, paddingCharacter);
        }

        return charactersWritten;
    }

    public void ContributeMaxCellSizes(int[] maxCellSizes)
    {
        if (cells.Length != maxCellSizes.Length)
            throw new InvalidOperationException(
                $"A row with {cells.Length} cells does not fit into a table of width {maxCellSizes.Length}");

        for (var i = 0; i < cells.Length; ++i)
        {
            maxCellSizes[i] = Math.Max(maxCellSizes[i], cells[i].Text.Length);
        }
    }

    private static int AppendJustified(StringBuilder builder, int maxCellWidth, string text, bool rightJustify,
        char paddingCharacter)
    {
        // Ensure text fits within cell and replace spaces with non-breaking space to stop
        // Discord shenanigans
        if (text.Length > maxCellWidth) text = text[..maxCellWidth];
        text = text.Replace(' ', '\u00a0');

        // Value if left justify
        if (!rightJustify) builder.Append(text);

        // Padding
        var paddingLength = maxCellWidth - text.Length;
        for (var j = 0; j < paddingLength; ++j) builder.Append(paddingCharacter);

        // Value if right justify
        if (rightJustify) builder.Append(text);

        // Return the number of characters we wrote
        return text.Length + paddingLength;
    }
}

