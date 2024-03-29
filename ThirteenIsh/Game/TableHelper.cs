using System.Text;

namespace ThirteenIsh.Game;

internal static class TableHelper
{
    public const string CellPadding = "..";
    public const string TablePadding = "   ";

    public static string BuildTable(int columnCount, IReadOnlyList<TableRowBase> data)
    {
        StringBuilder builder = new();
        BuildTableEx(builder, columnCount, data, true);
        return builder.ToString();
    }

    public static void BuildTableEx(
        StringBuilder builder, int columnCount, IReadOnlyList<TableRowBase> data,
        bool drawAsTwoColumnsIfPossible = true)
    {
        if (data.Count == 0) return;

        // Work out how wide each cell will be, and thence the whole table
        var maxCellSizes = new int[columnCount];
        foreach (var row in data)
        {
            row.ContributeMaxCellSizes(maxCellSizes);
        }

        builder.AppendLine("```");

        var tableWidth = maxCellSizes.Sum() + (maxCellSizes.Length - 1) * CellPadding.Length;
        if (drawAsTwoColumnsIfPossible && tableWidth < 30 - TablePadding.Length)
        {
            // Draw the table with two logical rows on each drawn row.
            var (halfRowsDiv, halfRowsRem) = Math.DivRem(data.Count, 2);
            var height = halfRowsDiv + halfRowsRem;
            for (var j = 0; j < height; ++j)
            {
                data[j].Append(builder, maxCellSizes);
                if (j + height < data.Count)
                {
                    builder.Append(TablePadding);
                    data[j + height].Append(builder, maxCellSizes);
                }

                builder.AppendLine();
            }
        }
        else
        {
            // Draw the table without rearranging like that.
            foreach (var row in data)
            {
                row.Append(builder, maxCellSizes);
                builder.AppendLine();
            }
        }

        builder.AppendLine("```");
    }
}
