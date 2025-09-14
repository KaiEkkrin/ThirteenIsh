# Stars Without Number Support

## Overview

ThirteenIsh includes support for the **Stars Without Number: Revised Edition** tabletop RPG system. This is a sci-fi OSR game that combines classic D&D-style mechanics with modern design elements for space adventure campaigns.

## Current Implementation Status

### ✅ Implemented Features

**Core Character System:**
- [x] Six attributes (Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma)
- [x] Attribute bonus calculations (-2 to +2 based on 3-18 range)
- [x] Character classes (Expert, Psychic, Warrior) with dual-class support
- [x] Level tracking (starts at 1)

**Skills System:**
- [x] All 19 core skills (Administer, Connect, Exert, Fix, Heal, Know, Lead, Notice, Perform, Pilot, Program, Punch, Shoot, Sneak, Stab, Survive, Talk, Trade, Work)
- [x] All 6 psychic skills (Biopsionics, Metapsionics, Precognition, Telekinesis, Telepathy, Teleportation)
- [x] Skill rolling (2d6 + skill bonus + attribute bonus)
- [x] Unskilled penalties (-1 skill level)

**Derived Statistics:**
- [x] Armor Class (10 + armor value + Dex bonus)
- [x] Attack Bonus (class and level dependent)
- [x] Hit Points (class-based with Con bonus, house rule for consistent HP)
- [x] Saving Throws (Physical, Mental, Evasion - calculated as 15 minus best relevant attribute bonus)
- [x] Effort (for psychic characters, based on class and psychic skills)

**Equipment:**
- [x] Armor Value tracking (base 10, modifiable for different armor types)

### ❌ Missing Features

## Core Character Creation

- [ ] **Character Backgrounds**
  - [ ] Background selection system
  - [ ] Starting skill packages per background
  - [ ] Background-specific character hooks and descriptions
  - [ ] Examples: Noble, Criminal, Spacer, Soldier, etc.

- [ ] **Character Foci**
  - [ ] Foci selection system (at levels 2, 5, 7, 10)
  - [ ] Foci abilities and special rules
  - [ ] Examples: Star Pilot, Doctor, Martial Artist, Gunslinger, Diplomat
  - [ ] Skill bonuses and special abilities from foci

## Character Advancement

- [ ] **Experience Points System**
  - [ ] XP tracking and storage
  - [ ] Level advancement triggers
  - [ ] Session-based XP awards

- [ ] **Level-Up Benefits**
  - [ ] Hit point re-rolling (take new roll or old+1, whichever is higher)
  - [ ] Automatic saving throw improvements (+1 each level)
  - [ ] Skill point allocation (3 points per level, +1 for Experts)
  - [ ] Foci selection at appropriate levels

## Combat & Encounter Systems

- [ ] **Initiative System**
  - [ ] 1d8 + Dex modifier initiative
  - [ ] Persistent initiative order (no re-rolling)
  - [ ] Initiative tracking in encounters

- [ ] **Combat Actions**
  - [ ] Action economy (Move, Main, Instant, On Turn actions)
  - [ ] Snap attacks (-4 penalty, consumes main action)
  - [ ] Execution attacks (skill check vs difficulty instead of to-hit)
  - [ ] Two-weapon fighting rules
  - [ ] Ranged vs melee interaction rules

- [ ] **Death & Dying**
  - [ ] 0 HP = 6 rounds to live
  - [ ] Stabilization mechanics (Int/Heal or Dex/Heal as Main Action)
  - [ ] Mortal wound system

## Advanced Character Features

- [ ] **Psychic Techniques**
  - [ ] Level-0 abilities for each psychic discipline
  - [ ] Higher-level psychic techniques (levels 1-4)
  - [ ] Effort expenditure and recovery mechanics
  - [ ] Psychic technique activation and targeting

- [ ] **Custom Counters**
  - [ ] Equipment tracking counters
  - [ ] Condition/status effect counters
  - [ ] Campaign-specific custom properties

## Character Display

- [x] **Character Summary**
  - [x] Basic character summary display (level and class info)
  - [ ] Class and background information (backgrounds not implemented yet)
  - [ ] Skills and foci summary (foci not implemented yet)
  - [ ] Equipment and statistics overview

## NPC/Monster Support

- [ ] **Monster System**
  - [ ] NPC stat blocks and properties
  - [ ] Monster-specific encounter rules
  - [ ] Different creature types (robots, aliens, etc.)

## Currently Throwing NotImplementedException

**SwnSystem.cs:**
- [ ] `EncounterAdd` (line 164)
- [ ] `EncounterBegin` (line 169)
- [ ] `EncounterJoin` (line 175)
- [x] `GetCharacterSummary` (line 180) - ✅ **COMPLETED**
- [ ] `EncounterNextRound` (line 185)

**SwnCharacterSystem.cs:**
- [ ] `BuildCustomCounter` (line 27)

## Implementation Notes

### House Rules Currently Applied
- **Hit Points**: Instead of rolling, base HP is 6 at level 1, plus 3.5 (rounded down) per additional level, plus (class bonus + Con bonus) × level
- **Classes**: Support for dual-classing (full Expert/Psychic, partial Warrior, etc.)

### Architecture Notes
- Game system follows the standard ThirteenIsh pattern with `GameSystem` and `CharacterSystem` base classes
- Counters use dependency injection for calculated values (e.g., AC depends on armor value and Dex bonus)
- Skills inherit from `SkillCounter` with SWN-specific rolling mechanics
- Saving throws are calculated counters based on attribute bonuses

## Development Priority

**High Priority** (Core gameplay):
1. Character backgrounds and starting packages
2. Basic encounter/combat system implementation

**Medium Priority** (Enhanced features):
1. Character foci system
2. Experience points and advancement
3. Psychic techniques

**Low Priority** (Advanced features):
1. Custom counters
2. Monster system
3. Advanced combat mechanics