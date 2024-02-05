namespace ThirteenIsh.Game.ThirteenthAge;

/// <summary>
/// Describes the 13th Age game system.
/// </summary>
internal sealed class ThirteenthAgeSystem : GameSystemBase
{
    private const string Level = "Level";

    private const string Strength = "Strength";
    private const string Dexterity = "Dexterity";
    private const string Constitution = "Constitution";
    private const string Intelligence = "Intelligence";
    private const string Wisdom = "Wisdom";
    private const string Charisma = "Charisma";

    public const string Barbarian = "Barbarian";
    public const string Bard = "Bard";
    public const string Cleric = "Cleric";
    public const string Fighter = "Fighter";
    public const string Paladin = "Paladin";
    public const string Ranger = "Ranger";
    public const string Rogue = "Rogue";
    public const string Sorcerer = "Sorcerer";
    public const string Wizard = "Wizard";

    public ThirteenthAgeSystem() : base("13th Age")
    {
        GameProperty classProperty = new("Class", Barbarian, Bard, Cleric, Fighter, Paladin, Ranger, Rogue, Sorcerer, Wizard);
        Properties = new GameProperty[]
        {
            classProperty
        };

        GameCounter levelCounter = new(Level, defaultValue: 1, minValue: 1, maxValue: 10);
        GameAbilityCounter strengthCounter = new(Strength);
        GameAbilityCounter dexterityCounter = new(Dexterity);
        GameAbilityCounter constitutionCounter = new(Constitution);
        GameAbilityCounter intelligenceCounter = new(Intelligence);
        GameAbilityCounter wisdomCounter = new(Wisdom);
        GameAbilityCounter charismaCounter = new(Charisma);

        AbilityBonusCounter strengthBonusCounter = new(strengthCounter);
        AbilityBonusCounter dexterityBonusCounter = new(dexterityCounter);
        AbilityBonusCounter constitutionBonusCounter = new(constitutionCounter);
        AbilityBonusCounter intelligenceBonusCounter = new(intelligenceCounter);
        AbilityBonusCounter wisdomBonusCounter = new(wisdomCounter);
        AbilityBonusCounter charismaBonusCounter = new(charismaCounter);

        HitPointsCounter hitPointsCounter = new(classProperty, levelCounter, constitutionBonusCounter);
        ArmorClassCounter armorClassCounter = new(classProperty, levelCounter,
            constitutionBonusCounter, dexterityBonusCounter, wisdomBonusCounter);

        PhysicalDefenseCounter physicalDefenseCounter = new(classProperty, levelCounter,
            strengthBonusCounter, dexterityBonusCounter, constitutionBonusCounter);

        MentalDefenseCounter mentalDefenseCounter = new(classProperty, levelCounter,
            intelligenceBonusCounter, wisdomBonusCounter, charismaBonusCounter);

        RecoveriesCounter recoveriesCounter = new();
        RecoveryDieCounter recoveryDieCounter = new(classProperty);

        // TODO allow for ad hoc bonuses for all the derived counters here?
        Counters = new GameCounter[]
        {
            levelCounter,
            strengthCounter,
            dexterityCounter,
            constitutionCounter,
            intelligenceCounter,
            wisdomCounter,
            charismaCounter,

            strengthBonusCounter,
            dexterityBonusCounter,
            constitutionBonusCounter,
            intelligenceBonusCounter,
            wisdomBonusCounter,
            charismaBonusCounter,

            hitPointsCounter,
            armorClassCounter,
            physicalDefenseCounter,
            mentalDefenseCounter,
            recoveriesCounter,
            recoveryDieCounter
        };

        Validate();
    }

    public override IReadOnlyList<GameProperty> Properties { get; }

    public override IReadOnlyList<GameCounter> Counters { get; }
}
