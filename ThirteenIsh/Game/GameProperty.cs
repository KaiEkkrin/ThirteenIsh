using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// A game property is an enumerated property of a character sheet, likely to
/// exist in the context of a particular game system. E.g. class or profession.
/// The corresponding entity is a CharacterProperty, stored in a CharacterSheet.
/// </summary>
internal class GameProperty(string name, params string[] possibleValues)
{
    public string Name => name;
    public IReadOnlyList<string> PossibleValues => possibleValues;

    public string GetValue(CharacterSheet characterSheet)
    {
        return characterSheet.Properties.First(o => o.Name == name).Value;
    }
}

