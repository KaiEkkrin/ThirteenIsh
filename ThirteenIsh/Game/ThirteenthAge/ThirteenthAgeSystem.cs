﻿using ThirteenIsh.Database;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;

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
    public const string ChaosMage = "Chaos Mage";
    public const string Commander = "Commander";
    public const string Druid = "Druid";
    public const string Fighter = "Fighter";
    public const string Monk = "Monk";
    public const string Necromancer = "Necromancer";
    public const string Occultist = "Occultist";
    public const string Paladin = "Paladin";
    public const string Ranger = "Ranger";
    public const string Rogue = "Rogue";
    public const string Sorcerer = "Sorcerer";
    public const string Wizard = "Wizard";

    internal const string EscalationDie = "EscalationDie";

    private ThirteenthAgeSystem(params CharacterSystem[] characterSystems) : base(SystemName, characterSystems)
    {
    }

    public static GameSystem Build()
    {
        GameProperty classProperty = new(Class,
                [
            Barbarian, Bard, Cleric, ChaosMage, Commander, Druid, Fighter, Monk, Necromancer,
            Occultist, Paladin, Ranger, Rogue, Sorcerer, Wizard
            ],
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

        var playerCharacterSystem = new ThirteenthAgeCharacterSystemBuilder(CharacterType.PlayerCharacter, SystemName)
            .AddPropertyGroup(basicsBuilder)
            .AddPropertyGroup(abilityScoresBuilder)
            .AddPropertyGroup(generalBuilder)
            .Build();

        // The monster counters are set directly in the sheet rather than being the derived counters that
        // they are for players
        var monsterStatsBuilder = new GamePropertyGroupBuilder(MonsterStats)
            .AddProperty(new MonsterInitiativeCounter())
            .AddProperty(new MonsterHitPointsCounter(HitPoints, HitPointsAlias))
            .AddProperty(new GameCounter(ArmorClass, ArmorClassAlias))
            .AddProperty(new GameCounter(PhysicalDefense, PhysicalDefenseAlias))
            .AddProperty(new GameCounter(MentalDefense, MentalDefenseAlias));

        var monsterSystem = new ThirteenthAgeCharacterSystemBuilder(CharacterType.Monster, SystemName)
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

    public override EncounterRollResult EncounterAdd(
        DataContext dataContext,
        Character character,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        int swarmCount,
        ulong userId)
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
            SwarmCount = swarmCount,
            UserId = userId
        };

        var characterSystem = GetCharacterSystem(CharacterType.Monster);
        characterSystem.ResetVariables(combatant);

        // Roll its initiative
        var initiative = RollMonsterInitiative(characterSystem, combatant, encounter, random, rerolls, userId);
        if (initiative.Error != GameCounterRollError.Success)
            return EncounterRollResult.BuildError(initiative);

        // Add it to the encounter
        combatant.Initiative = initiative.Roll;
        combatant.InitiativeRollWorking = initiative.Working;
        encounter.InsertCombatantIntoTurnOrder(combatant);
        return EncounterRollResult.BuildSuccess(initiative, combatant.Alias);
    }

    public override void EncounterBegin(Encounter encounter)
    {
        encounter.State.Counters.SetValue(EscalationDie, 0);
    }

    public override EncounterRollResult EncounterJoin(
        DataContext dataContext,
        Adventurer adventurer,
        Encounter encounter,
        NameAliasCollection nameAliasCollection,
        IRandomWrapper random,
        int rerolls,
        ulong userId)
    {
        var dexterityBonusCounter = GetCharacterSystem(CharacterType.PlayerCharacter)
            .GetProperty<GameCounter>(adventurer.Sheet, AbilityBonusCounter.GetBonusCounterName(Dexterity));

        int? targetValue = null;
        var initiative = dexterityBonusCounter.Roll(adventurer, null, random, rerolls, ref targetValue);
        if (initiative.Error != GameCounterRollError.Success)
            return EncounterRollResult.BuildError(initiative);

        AdventurerCombatant combatant = new()
        {
            Alias = nameAliasCollection.Add(adventurer.Name, 10, false),
            Initiative = initiative.Roll,
            InitiativeRollWorking = initiative.Working,
            Name = adventurer.Name,
            UserId = userId
        };

        encounter.InsertCombatantIntoTurnOrder(combatant);
        return EncounterRollResult.BuildSuccess(initiative, combatant.Alias);
    }

    public override string GetCharacterSummary(CharacterSheet sheet, CharacterType type)
    {
        var characterSystem = GetCharacterSystem(type);
        switch (type)
        {
            case CharacterType.PlayerCharacter:
                var characterClass = characterSystem.GetProperty<GameProperty>(sheet, Class).GetValue(sheet);
                var level = characterSystem.GetProperty<GameCounter>(sheet, Level).GetValue(sheet);
                return $"Level {level} {characterClass}";

            case CharacterType.Monster:
                // TODO Add a monster type and level or what have you to display here
                return "Monster";

            default:
                throw new ArgumentException("Unrecognised character type", nameof(type));
        }
    }

    public override string GetCharacterSummary(ITrackedCharacter character)
    {
        var sheet = character.Sheet;
        var characterSystem = GetCharacterSystem(character.Type);
        switch (character.Type)
        {
            case CharacterType.PlayerCharacter:
                var characterClass = characterSystem.GetProperty<GameProperty>(sheet, Class).GetValue(sheet);
                var level = characterSystem.GetProperty<GameCounter>(sheet, Level).GetValue(character);
                return $"Level {level} {characterClass}";

            case CharacterType.Monster:
                // TODO Add a monster type and level or what have you to display here
                return "Monster";

            default:
                throw new ArgumentException("Unrecognised character type", nameof(character));
        }
    }

    protected override void AddEncounterHeadingRow(List<TableRow> data, Encounter encounter)
    {
        base.AddEncounterHeadingRow(data, encounter);
        data.Add(new TableRow(new TableCell("Escalation Die"), TableCell.Integer(
            encounter.State.Counters.TryGetValue(EscalationDie, out var escalationDieValue) ? escalationDieValue : 0)));
    }

    protected override CombatantBase? EncounterNextRound(Encounter encounter, IRandomWrapper random)
    {
        // When we roll over to the next round, increase the escalation die, to a maximum of 6.
        // TODO Add commands to explicitly set and modify an encounter variable?
        encounter.State.Counters.SetValue(EscalationDie,
            Math.Min(6, (encounter.State.Counters.TryGetValue(EscalationDie, out var escalationDieValue)
                ? escalationDieValue : 0) + 1));

        return encounter.GetCurrentCombatant();
    }

    private static GameCounterRollResult RollMonsterInitiative(CharacterSystem characterSystem, MonsterCombatant combatant,
        Encounter encounter, IRandomWrapper random, int rerolls, ulong userId)
    {
        var matchingMonster = encounter.Combatants.OfType<MonsterCombatant>()
            .FirstOrDefault(c => c.Name == combatant.Name && c.UserId == userId);

        var initiativeCounter = characterSystem.GetProperty<GameCounter>(combatant.Sheet, Initiative);
        if (matchingMonster is { Initiative: { } roll, InitiativeRollWorking: { } working })
        {
            // We have already rolled for this monster type -- re-use the same one.
            return new GameCounterRollResult
            { CounterName = initiativeCounter.Name, Error = GameCounterRollError.Success, Roll = roll, Working = working };
        }

        int? targetValue = null;
        var initiative = initiativeCounter.Roll(combatant, null, random, rerolls, ref targetValue);
        return initiative;
    }
}
