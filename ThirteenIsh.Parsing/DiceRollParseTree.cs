namespace ThirteenIsh.Parsing;

[DebuggerDisplay("{AsString}")]
public sealed class DiceRollParseTree(int offset, int diceCount, int diceSign, int diceSize,
    int? keepHighest = null, int? keepLowest = null)
    : ParseTreeBase(offset)
{
    public string AsString => ToString();

    public static DiceRollParseTree BuildWithRerolls(int diceSize, int rerolls, int baseDiceCount = 1)
    {
        int? keepHighest = rerolls >= 1 ? baseDiceCount : null;
        int? keepLowest = rerolls <= -1 ? baseDiceCount : null;
        return new DiceRollParseTree(
            0, Math.Abs(rerolls) + baseDiceCount, 1, diceSize, keepHighest, keepLowest);
    }

    public override int Evaluate(IRandomWrapper random, out string working)
    {
        // I want to illustrate the rolls in the natural order they came out,
        // with strikethrough for any rejected by `keepLowest` or `keepHighest`
        // in the working:
        var rolls = new int[diceCount];
        for (var i = 0; i < diceCount; ++i)
        {
            var roll = random.NextInteger(1, diceSize + 1);
            rolls[i] = roll;
        }

        var keepCounts = (keepHighest, keepLowest) switch
        {
            ({ } highestCount, null) => BuildKeepDictionary(rolls.OrderByDescending(r => r).Take(highestCount)),
            (null, { } lowestCount) => BuildKeepDictionary(rolls.OrderBy(r => r).Take(lowestCount)),
            (null, null) => null,
            _ => throw new NotSupportedException(
                "DiceRollParseTree does not support both keepHighest and keepLowest at once")
        };

        var diceSignStringPart = diceSign switch
        {
            -1 => "-",
            1 => string.Empty,
            _ => throw new NotSupportedException("DiceRollParseTree accepts only diceSign of -1 or 1")
        };

        StringBuilder workingBuilder = new();
        workingBuilder.Append(CultureInfo.CurrentCulture, $"{diceSignStringPart}{diceCount}d{diceSize}");
        if (keepHighest.HasValue)
        {
            workingBuilder.Append(CultureInfo.CurrentCulture, $"k{keepHighest}");
        }
        else if (keepLowest.HasValue)
        {
            workingBuilder.Append(CultureInfo.CurrentCulture, $"l{keepLowest}");
        }

        var sum = diceSign * DoSummation(rolls, keepCounts, out var sumWorking);
        workingBuilder.Append(CultureInfo.CurrentCulture,
            $" 🎲 {sum}{sumWorking}");

        working = workingBuilder.ToString();
        return sum;
    }

    public override string ToString() => (keepHighest, keepLowest) switch
    {
        ({ } highestCount, null) => $"{diceCount}d{diceSize}k{highestCount}",
        (null, { } lowestCount) => $"{diceCount}d{diceSize}l{lowestCount}",
        (null, null) => $"{diceCount}d{diceSize}",
        _ => throw new NotSupportedException(
            "DiceRollParseTree does not support both keepHighest and keepLowest at once")
    };

    // This might seem overblown but I think it's needed in order to get the working output
    // I want, in dice roll order with the later ones struck through and the earlier ones kept
    // if there are multiple equal rolls within the lowest or highest bounds :)
    private static Dictionary<int, int> BuildKeepDictionary(IEnumerable<int> ints)
    {
        Dictionary<int, int> dictionary = [];
        foreach (var value in ints)
        {
            if (dictionary.TryGetValue(value, out var count))
            {
                dictionary[value] = count + 1;
            }
            else
            {
                dictionary.Add(value, 1);
            }
        }

        return dictionary;
    }

    private int DoSummation(int[] rolls, Dictionary<int, int>? keepCounts, out string working)
    {
        var sum = 0;
        StringBuilder workingBuilder = new();
        if (diceCount > 1) workingBuilder.Append(" [");
        for (var i = 0; i < rolls.Length; ++i)
        {
            if (keepCounts is null)
            {
                // No keeping and dropping, just sum everything.
                sum += rolls[i];
                AppendRoll(i);
            }
            else if (keepCounts.TryGetValue(rolls[i], out var count))
            {
                sum += rolls[i];
                AppendRoll(i);
                if (count > 1)
                {
                    keepCounts[rolls[i]] = count - 1;
                }
                else
                {
                    keepCounts.Remove(rolls[i]);
                }
            }
            else
            {
                // Show this as strikethrough in the working
                AppendRoll(i, "~~");
            }
        }

        // Sanity check : we should have accounted for all rolls to keep by now
        if (keepCounts is { Count: > 0 })
            throw new InvalidOperationException(
                $"After summation: {keepCounts.Values.Sum()} values un-accounted for");

        if (diceCount > 1) workingBuilder.Append(']');
        working = workingBuilder.ToString();
        return sum;

        void AppendRoll(int i, string? formatting = null)
        {
            if (diceCount == 1) return;
            if (i > 0)
            {
                workingBuilder.Append(CultureInfo.CurrentCulture, $" + {formatting}{rolls[i]}{formatting}");
            }
            else
            {
                workingBuilder.Append(CultureInfo.CurrentCulture, $"{formatting}{rolls[i]}{formatting}");
            }
        }
    }
}
