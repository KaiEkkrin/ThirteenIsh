using System.Text;

namespace ThirteenIsh.Game;

internal static class TableHelper
{
    public const int CellPaddingLength = 2;
    public const int TablePaddingLength = 3;

    public const int MaxPinnedTableWidth = 36;

    public static string BuildTable(int columnCount, IReadOnlyList<TableRowBase> data)
    {
        StringBuilder builder = new();
        BuildTableEx(builder, columnCount, data);
        return builder.ToString();
    }

    public static void BuildTableEx(
        StringBuilder builder, int columnCount, IReadOnlyList<TableRowBase> data,
        bool drawAsTwoColumnsIfPossible = true,
        char cellPaddingCharacter = '\u00b7', // middle dot
        char tablePaddingCharacter = '\u00a0', // non-breaking space
        int maxTableWidth = 40)
    {
        if (data.Count == 0) return;

        // Work out how wide each cell will be, and thence the whole table
        var maxCellSizes = new int[columnCount];
        foreach (var row in data)
        {
            row.ContributeMaxCellSizes(maxCellSizes);
        }

        builder.Append("```");

        var tableWidth = maxCellSizes.Sum() + (maxCellSizes.Length - 1) * CellPaddingLength;
        var charactersInLine = 0;
        if (drawAsTwoColumnsIfPossible && tableWidth < maxTableWidth / 2 - TablePaddingLength)
        {
            // Draw the table with two logical rows on each drawn row.
            var (halfRowsDiv, halfRowsRem) = Math.DivRem(data.Count, 2);
            var height = halfRowsDiv + halfRowsRem;
            for (var j = 0; j < height; ++j)
            {
                builder.AppendLine();
                charactersInLine = data[j].Append(builder, maxCellSizes, cellPaddingCharacter);
                if (j + height < data.Count)
                {
                    for (var k = 0; k < TablePaddingLength; ++k) builder.Append(tablePaddingCharacter);
                    charactersInLine += TablePaddingLength +
                        data[j + height].Append(builder, maxCellSizes, cellPaddingCharacter);
                }
            }
        }
        else
        {
            // Draw the table without rearranging like that.
            foreach (var row in data)
            {
                builder.AppendLine();
                charactersInLine = row.Append(builder, maxCellSizes, cellPaddingCharacter);
            }
        }

        // HACK : To discourage Discord from randomly line breaking my tables in ridiculous
        // places, pad the last line with a whole lot of nbsp. This will wrap, but only once.
        // (Discord's ability to format text into tables seems completely knackered.)
        for (var i = charactersInLine; i < maxTableWidth; ++i) builder.Append('\u00a0');

        builder.AppendLine("```");
    }
}
