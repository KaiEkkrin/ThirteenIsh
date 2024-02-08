﻿using System.Text;

namespace ThirteenIsh;

internal class DiscordUtil
{
    public static string BuildTable(
        int columnCount, IEnumerable<string[]> data, params int[] rightJustifiedColumns)
    {
        var rightJustify = new bool[columnCount];
        foreach (var column in rightJustifiedColumns)
        {
            rightJustify[column] = true;
        }

        // Work out how wide each cell will be, and thence the whole table
        List<string[]> dataList = [];
        var maxCellSizes = new int[columnCount];
        foreach (var row in data)
        {
            if (row.Length != columnCount)
                throw new ArgumentException($"Found row with {row.Length} columns, expected {columnCount}", nameof(data));

            for (var i = 0; i < columnCount; ++i)
            {
                maxCellSizes[i] = Math.Max(maxCellSizes[i], row[i].Length);
            }

            dataList.Add(row);
        }

        StringBuilder builder = new();
        builder.AppendLine("```");

        const string cellPadding = "..";
        const string tablePadding = "   ";
        var tableWidth = maxCellSizes.Sum() + (maxCellSizes.Length - 1) * cellPadding.Length;
        if (tableWidth < 30 - tablePadding.Length)
        {
            // Draw the table with two logical rows on each drawn row.
            var (halfRowsDiv, halfRowsRem) = Math.DivRem(dataList.Count, 2);
            var height = halfRowsDiv + halfRowsRem;
            var leftData = dataList[..height];
            var rightData = dataList[height..];

            for (var j = 0; j < height; ++j)
            {
                AppendDataRow(leftData[j]);
                if (j < rightData.Count)
                {
                    builder.Append(tablePadding);
                    AppendDataRow(rightData[j]);
                }

                builder.AppendLine();
            }
        }
        else
        {
            // Draw the table without rearranging like that.
            foreach (var row in dataList)
            {
                AppendDataRow(row);
                builder.AppendLine();
            }
        }

        builder.AppendLine("```");
        return builder.ToString();

        void AppendDataRow(string[] row)
        {
            for (var i = 0; i < columnCount; ++i)
            {
                // Between-cell padding
                if (i > 0) builder.Append(cellPadding);

                // Value if left justify
                if (!rightJustify[i]) builder.Append(row[i]);

                // Padding
                var paddingLength = maxCellSizes[i] - row[i].Length;
                for (var j = 0; j < paddingLength; ++j) builder.Append('.');

                // Value if right justify
                if (rightJustify[i]) builder.Append(row[i]);
            }
        }
    }
}
