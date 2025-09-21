namespace ThirteenIsh.Game;

/// <summary>
/// Flags that modify how a GameCounter.Roll operation behaves.
/// </summary>
[Flags]
public enum GameCounterRollOptions
{
    /// <summary>
    /// No special behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// This is an attack roll, which may use different dice mechanics
    /// (e.g., 1d20 instead of 2d6 for SWN skill checks).
    /// </summary>
    IsAttack = 1
}