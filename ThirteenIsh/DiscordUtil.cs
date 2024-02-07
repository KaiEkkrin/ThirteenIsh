using System.Text;

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
        foreach (var row in dataList)
        {
            for (var i = 0; i < columnCount; ++i)
            {
                // Between-cell padding
                if (i > 0) builder.Append('.');

                // Value if left justify
                if (!rightJustify[i]) builder.Append(row[i]);

                // Padding
                var paddingLength = maxCellSizes[i] - row.Length;
                for (var j = 0; j < paddingLength; ++j) builder.Append('.');

                // Value if right justify
                if (rightJustify[i]) builder.Append(row[i]);
            }

            builder.AppendLine();
        }

        builder.AppendLine("```");
        return builder.ToString();
    }
}
