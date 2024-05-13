using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Services;

namespace ThirteenIsh.Game.ThirteenthAge;

/// <summary>
/// Describes the 13th Age game system.
/// </summary>
internal sealed class ThirteenthAgeSystem : GameSystem
{
    public const string SystemName = "13th Age";

    public const string Basics = "Basics";
    public const string AbilityScores = "Ability Scores";
    public const string General = "General";
    public const string MonsterStats = "Monster Stats";

    public const string Class = "Class";
    public const string Level = "Level";

    public const string Strength = "Strength";
    public const string Dexterity = "Dexterity";
    public const string Constitution = "Constitution";
    public const string Intelligence = "Intelligence";
    public const string Wisdom = "Wisdom";
    public const string Charisma = "Charisma";

    public const string Initiative = "Initiative"; // only for monsters :)

    public const string HitPoints = "Hit Points";
    public const string HitPointsAlias = "HP";

    public const string ArmorClass = "Armor Class";
    public const string ArmorClassAlias = "AC";

    public const string PhysicalDefense = "Physical Defense";
    public const string PhysicalDefenseAlias = "PD";

    public const string MentalDefense = "Mental Defense";
    public const string MentalDefenseAlias = "MD";

    public const string Barbarian = "Barbarian";
    public const string Bard = "Bard";
    public const string Cleric = "Cleric";
    public const string Fighter = "Fighter";
    public const string Paladin = "Paladin";
    public const string Ranger = "Ranger";
    public const string Rogue = "Rogue";
    public const string Sorcerer = "Sorcerer";
    public const string Wizard = "Wizard";

    private const string EscalationDie = "EscalationDie";

    private ThirteenthAgeSystem(params CharacterSystem[] characterSystems) : base(SystemName, characterSystems)
    {
    }

    public static GameSystem Build()
    {
        GameProperty classProperty = new(Class,
                [Barbarian, Bard, Cleric, Fighter, Paladin, Ranger, Rogue, Sorcerer, Wizard],
                true);

        GameCounter levelCounter = new(Level, defaultValue: 1, minValue: 1, maxValue: 10);

        var basicsBuilder = new GamePropertyGroupBuilder(Basics)
            .AddProperties(classProperty, levelCounter);

        GamePropertyGroupBuilder abilityScoresBuilder = new(AbilityScores);

        var strengthBonusCounter = BuildAbility(abilityScoresBuilder, Strength, levelCounter);
        var dexterityBonusCounter = BuildAbility(abilityScoresBuilder, Dexterity, levelCounter);
        var constitutionBonusCounter = BuildAbility(abilityScoresBuilder, Constitution, levelCounter);
        var intelligenceBonusCounter = BuildAbility(abilityScoresBuilder, Intelligence, levelCounter);
        var wisdomBonusCounter = BuildAbility(abilityScoresBuilder, Wisdom, levelCounter);
        var charismaBonusCounter = BuildAbility(abilityScoresBuilder, Charisma, levelCounter);

        HitPointsCounter hitPointsCounter = new(classProperty, levelCounter, constitutionBonusCounter);
        var generalBuilder = new GamePropertyGroupBuilder(General)
            .AddProperty(hitPointsCounter)
            .AddProperty(new ArmorClassCounter(classProperty, levelCounter, constitutionBonusCounter, dexterityBonusCounter,
                wisdomBonusCounter))
            .AddProperty(new PhysicalDefenseCounter(classProperty, levelCounter, strengthBonusCounter, dexterityBonusCounter,
                constitutionBonusCounter))
            .AddProperty(new MentalDefenseCounter(classProperty, levelCounter, intelligenceBonusCounter, wisdomBonusCounter,
                charismaBonusCounter))
            .AddProperty(new RecoveriesCounter())
            .AddProperty(new RecoveryDieCounter(classProperty));

        var playerCharacterSystem = new CharacterSystemBuilder(CharacterType.PlayerCharacter, SystemName)
            .AddPropertyGroup(basicsBuilder)
            .AddPropertyGroup(abilityScoresBuilder)
            .AddPropertyGroup(generalBuilder)
            .Build();

        // The monster counters are set directly in the sheet rather than being the derived counters that
        // they are for players
        var monsterStatsBuilder = new GamePropertyGroupBuilder(MonsterStats)
            .AddProperty(new MonsterInitiativeCounter())
            .AddProperty(new GameCounter(HitPoints, HitPointsAlias, options: GameCounterOptions.HasVariable))
            .AddProperty(new GameCounter(ArmorClass, ArmorClassAlias))
            .AddProperty(new GameCounter(PhysicalDefense, PhysicalDefenseAlias))
            .AddProperty(new GameCounter(MentalDefense, MentalDefenseAlias));

        var monsterSystem = new CharacterSystemBuilder(CharacterType.Monster, SystemName)
            .AddPropertyGroup(monsterStatsBuilder)
            .Build();

        return new ThirteenthAgeSystem(playerCharacterSystem, monsterSystem);
    }

    private static AbilityBonusCounter BuildAbility(GamePropertyGroupBuilder builder, string abilityName,
        GameCounter levelCounter)
    {
        GameAbilityCounter counter = new(abilityName);
        AbilityBonusCounter bonusCounter = new(levelCounter, counter);
        builder.AddProperty(counter).AddProperty(bonusCounter);
        return bonusCounter;
    }

    public override GameCounterRollResult? EncounterAdd(
        DataContext dataContext,
        Character character,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId,
        out string alias)
    {
        if (character.CharacterType != CharacterType.Monster)
            throw new ArgumentException("EncounterAdd requires a monster", nameof(character));

        // Set up a new combatant for this monster. We'll assign the initiative values in a moment.
        MonsterCombatant combatant = new()
        {
            Alias = nameAliasCollection.Add(character.Name, 5, true),
            LastUpdated = DateTimeOffset.UtcNow,
            Name = character.Name,
            Sheet = character.Sheet,
            UserId = userId
        };

        var characterSystem = GetCharacterSystem(CharacterType.Monster);
        characterSystem.ResetVariables(combatant);

        // Roll its initiative
        var initiative = RollMonsterInitiative(characterSystem, combatant, encounter, random, rerolls, userId);

        // Add it to the encounter
        combatant.Initiative = initiative.Roll;
        combatant.InitiativeRollWorking = initiative.Working;

        dataContext.Combatants.Add(combatant);
        encounter.InsertCombatantIntoTurnOrder(combatant);
        alias = combatant.Alias;
        return initiative;
    }

    public override void EncounterBegin(Encounter encounter)
    {
        encounter.Variables.Counters.SetValue(EscalationDie, 0);
    }

    public override GameCounterRollResult? EncounterJoin(
        DataContext dataContext,
        Adventurer adventurer,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId)
    {
        var dexterityBonusCounter = GetCharacterSystem(CharacterType.PlayerCharacter)
            .GetProperty<GameCounter>(AbilityBonusCounter.GetBonusCounterName(Dexterity));

        int? targetValue = null;
        var initiative = dexterityBonusCounter.Roll(adventurer, null, random, rerolls, ref targetValue);

        AdventurerCombatant combatant = new()
        {
            Alias = nameAliasCollection.Add(adventurer.Name, 10, false),
            Encounter = encounter,
            Initiative = initiative.Roll,
            InitiativeRollWorking = initiative.Working,
            Name = adventurer.Name,
            UserId = userId
        };

        dataContext.Combatants.Add(combatant);
        encounter.InsertCombatantIntoTurnOrder(combatant);
        return initiative;
    }

    public override string GetCharacterSummary(CharacterSheet sheet, CharacterType type)
    {
        var characterSystem = GetCharacterSystem(type);
        switch (type)
        {
            case CharacterType.PlayerCharacter:
                var characterClass = characterSystem.GetProperty<GameProperty>(Class).GetValue(sheet);
                var level = characterSystem.GetProperty<GameCounter>(Level).GetValue(sheet);
                return $"Level {level} {characterClass}";

            case CharacterType.Monster:
                // TODO Add a monster type and level or what have you to display here
                return "Monster";

            default:
                throw new ArgumentException("Unrecognised character type", nameof(type));
        }
    }

    protected override void AddEncounterHeadingRow(List<TableRowBase> data, Encounter encounter)
    {
        base.AddEncounterHeadingRow(data, encounter);
        data.Add(new TableRow(new TableCell("Escalation Die"), TableCell.Integer(
            encounter.Variables.Counters.TryGetValue(EscalationDie, out var escalationDieValue) ? escalationDieValue : 0)));
    }

    protected override async Task BuildEncounterInitiativeTableRowsAsync(
        SqlDataService dataService,
        Adventure adventure,
        CombatantBase combatant,
        EncounterInitiativeTableBuilder builder,
        CancellationToken cancellationToken = default)
    {
        var hitPointsCounter = GetCharacterSystem(combatant.CharacterType).GetProperty<GameCounter>(HitPoints);
        var hitPointsCell = await BuildPointsEncounterTableCellAsync(dataService, combatant, hitPointsCounter,
            cancellationToken);

        builder.AddRow(
            new TableCell(hitPointsCounter.Alias ?? hitPointsCounter.Name),
            new TableCell(hitPointsCell));
    }

    protected override CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // When we roll over to the next round, increase the escalation die, to a maximum of 6.
        // TODO Add commands to explicitly set and modify an encounter variable?
        encounter.Variables.Counters.SetValue(EscalationDie,
            Math.Min(6, (encounter.Variables.Counters.TryGetValue(EscalationDie, out var escalationDieValue)
                ? escalationDieValue : 0) + 1));

        return encounter.GetCurrentCombatant();
    }

    private static GameCounterRollResult RollMonsterInitiative(CharacterSystem characterSystem, MonsterCombatant combatant,
        Encounter encounter, IRandomWrapper random, int rerolls, ulong userId)
    {
        var matchingMonster = encounter.Combatants.OfType<MonsterCombatant>()
            .FirstOrDefault(c => c.Name == combatant.Name && c.UserId == userId);

        if (matchingMonster is { Initiative: { } roll, InitiativeRollWorking: { } working })
        {
            // We have already rolled for this monster type -- re-use the same one.
            return new GameCounterRollResult { Roll = roll, Working = working };
        }

        var initiativeCounter = characterSystem.GetProperty<GameCounter>(Initiative);
        int? targetValue = null;
        var initiative = initiativeCounter.Roll(combatant, null, random, rerolls, ref targetValue);
        return initiative;
    }
}
