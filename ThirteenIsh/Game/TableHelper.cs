using System.Text;

namespace ThirteenIsh.Game;

internal static class TableHelper
{
    public const int CellPaddingLength = 2;
    public const int TablePaddingLength = 3;

    public static string BuildTable(int columnCount, IReadOnlyList<TableRowBase> data)
    {
        StringBuilder builder = new();
        BuildTableEx(builder, columnCount, data, true);
        return builder.ToString();
    }

    public static void BuildTableEx(
        StringBuilder builder, int columnCount, IReadOnlyList<TableRowBase> data,
        bool drawAsTwoColumnsIfPossible = true,
        char cellPaddingCharacter = '.',
        char tablePaddingCharacter = ' ')
    {
        if (data.Count == 0) return;

        // Work out how wide each cell will be, and thence the whole table
        var maxCellSizes = new int[columnCount];
        foreach (var row in data)
        {
            row.ContributeMaxCellSizes(maxCellSizes);
        }

        builder.AppendLine("```");

        var tableWidth = maxCellSizes.Sum() + (maxCellSizes.Length - 1) * CellPaddingLength;
        if (drawAsTwoColumnsIfPossible && tableWidth < 30 - TablePaddingLength)
        {
            // Draw the table with two logical rows on each drawn row.
            var (halfRowsDiv, halfRowsRem) = Math.DivRem(data.Count, 2);
            var height = halfRowsDiv + halfRowsRem;
            for (var j = 0; j < height; ++j)
            {
                data[j].Append(builder, maxCellSizes, cellPaddingCharacter);
                if (j + height < data.Count)
                {
                    for (var k = 0; k < TablePaddingLength; ++k) builder.Append(tablePaddingCharacter);
                    data[j + height].Append(builder, maxCellSizes, cellPaddingCharacter);
                }

                builder.AppendLine();
            }
        }
        else
        {
            // Draw the table without rearranging like that.
            foreach (var row in data)
            {
                row.Append(builder, maxCellSizes, cellPaddingCharacter);
                builder.AppendLine();
            }
        }

        builder.AppendLine("```");
    }
}
