﻿using ThirteenIsh.Entities;

namespace ThirteenIsh.Game;

/// <summary>
/// Base class for custom logic for the game system that does known things.
/// </summary>
internal abstract class GameSystemLogicBase
{
    /// <summary>
    /// Provides a one-line summary of the character for character list purposes.
    /// </summary>
    public abstract string GetCharacterSummary(CharacterSheet sheet);
}