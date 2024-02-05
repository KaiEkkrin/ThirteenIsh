namespace ThirteenIsh.Game.Dragonbane;

internal sealed class DragonbaneSystem : GameSystemBase
{
    public const string Human = "Human";
    public const string Halfling = "Halfling";
    public const string Dwarf = "Dwarf";
    public const string Elf = "Elf";
    public const string Mallard = "Mallard";
    public const string Wolfkin = "Wolfkin";

    public const string Artisan = "Artisan";
    public const string Bard = "Bard";
    public const string Fighter = "Fighter";
    public const string Hunter = "Hunter";
    public const string Knight = "Knight";
    public const string Mage = "Mage";
    public const string Mariner = "Mariner";
    public const string Merchant = "Merchant";
    public const string Scholar = "Scholar";
    public const string Thief = "Thief";

    public const string Strength = "Strength";
    public const string Constitution = "Constitution";
    public const string Agility = "Agility";
    public const string Intelligence = "Intelligence";
    public const string Willpower = "Willpower";
    public const string Charisma = "Charisma";

    public DragonbaneSystem() : base("Dragonbane")
    {
        GameProperty kinProperty = new("Kin", Human, Halfling, Dwarf, Elf, Mallard, Wolfkin);
        GameProperty professionProperty = new("Profession", Artisan, Bard, Fighter, Hunter, Knight,
            Mage, Mariner, Merchant, Scholar, Thief);

        GameAbilityCounter strengthCounter = new(Strength, maxValue: 18);
        GameAbilityCounter constitutionCounter = new(Constitution, maxValue: 18);
        GameAbilityCounter agilityCounter = new(Agility, maxValue: 18);
        GameAbilityCounter intelligenceCounter = new(Intelligence, maxValue: 18);
        GameAbilityCounter willpowerCounter = new(Willpower, maxValue: 18);
        GameAbilityCounter charismaCounter = new(Charisma, maxValue: 18);

        Properties = new[]
        {
            kinProperty,
            professionProperty
        };

        // Derived counters
        MovementCounter movementCounter = new(kinProperty, agilityCounter);
        PointsCounter hitPointsCounter = new("Hit Points", "HP", constitutionCounter);
        PointsCounter willpowerPointsCounter = new("Willpower Points", "WP", willpowerCounter);

        DamageBonusCounter strengthDamageBonusCounter = new("Strength Damage Bonus", strengthCounter);
        DamageBonusCounter agilityDamageBonusCounter = new("Agility Damage Bonus", agilityCounter);

        var counters = new List<GameCounter>
        {
            strengthCounter,
            constitutionCounter,
            agilityCounter,
            intelligenceCounter,
            willpowerCounter,
            charismaCounter,

            movementCounter,
            hitPointsCounter,
            willpowerPointsCounter,

            strengthDamageBonusCounter,
            agilityDamageBonusCounter
        };

        List<GameCounter> skillCounters = [];
        List<GameCounter> skillLevelCounters = [];
        List<GameCounter> equipmentCounters = [];

        BuildSkill("Acrobatics", agilityCounter);
        BuildSkill("Awareness", intelligenceCounter);
        BuildSkill("Bartering", charismaCounter);
        BuildSkill("Beast Lore", intelligenceCounter);
        BuildSkill("Bluffing", charismaCounter);
        BuildSkill("Bushcraft", intelligenceCounter);
        BuildSkill("Crafting", strengthCounter);
        BuildSkill("Evade", agilityCounter);
        BuildSkill("Healing", intelligenceCounter);
        BuildSkill("Hunting & Fishing", agilityCounter);
        BuildSkill("Languages", intelligenceCounter);
        BuildSkill("Myths & Legends", intelligenceCounter);
        BuildSkill("Performance", charismaCounter);
        BuildSkill("Persuasion", charismaCounter);
        BuildSkill("Riding", agilityCounter);
        BuildSkill("Seamanship", intelligenceCounter);
        BuildSkill("Sleight of Hand", agilityCounter);
        BuildSkill("Sneaking", agilityCounter);
        BuildSkill("Spot Hidden", intelligenceCounter);
        BuildSkill("Swimming", agilityCounter);

        BuildSkill("Axes", strengthCounter);
        BuildSkill("Bows", agilityCounter);
        BuildSkill("Brawling", strengthCounter);
        BuildSkill("Crossbows", agilityCounter);
        BuildSkill("Hammers", strengthCounter);
        BuildSkill("Knives", agilityCounter);
        BuildSkill("Slings", agilityCounter);
        BuildSkill("Spears", strengthCounter);
        BuildSkill("Staves", agilityCounter);
        BuildSkill("Swords", strengthCounter);

        BuildSkill("Animism", intelligenceCounter, true);
        BuildSkill("Elementalism", intelligenceCounter, true);
        BuildSkill("Mentalism", intelligenceCounter, true);

        // The variables to these track durability.
        // Players will need to make a custom counter for weapon durability ;)
        GameCounter armorCounter = new("Armor", hasVariable: true);
        GameCounter helmetCounter = new("Helmet", hasVariable: true);

        equipmentCounters.Add(armorCounter);
        equipmentCounters.Add(helmetCounter);

        counters.AddRange(skillCounters);
        counters.AddRange(skillLevelCounters);
        counters.AddRange(equipmentCounters);
        Counters = counters;

        Validate();

        void BuildSkill(string name, GameAbilityCounter attributeCounter, bool secondary = false)
        {
            GameCounter skillCounter = new(name);
            SkillLevelCounter skillLevelCounter = new(attributeCounter, skillCounter, secondary);

            skillCounters.Add(skillCounter);
            skillLevelCounters.Add(skillLevelCounter);
        }
    }

    public override IReadOnlyList<GameProperty> Properties { get; }

    public override IReadOnlyList<GameCounter> Counters { get; }
}
