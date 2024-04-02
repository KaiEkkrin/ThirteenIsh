# ThirteenIsh

An experimental Discord bot for dice rolling and TTRPG tracking

## Setup

I followed the instructions in [Discord.net -- Your First Bot](https://discordnet.dev/guides/getting_started/first-bot.html) and in [Discord.net -- Getting started with application commands](https://discordnet.dev/guides/int_basics/application-commands/intro.html). The bot will need to be added to servers with the following permissions:

* bot
* applications.commands

And, it will need these bot permissions:

* Manage Messages (8192)

## Database

ThirteenIsh uses MongoDB. You can run up a basic Docker container:

```powershell
docker run --memory=4g --name thirteenish-mongo --restart unless-stopped -d -p 27017:27017 -v thirteenish-mongo-data:/data/db mongo:5
```

If you're using that, you can use the connection string `mongodb://localhost:27017`

## Configuration settings

Configure these via command line, environment variables or user secrets.

* "BotToken": the Discord bot token.
* "MongoConnectionString": the MongoDB connection string.

## Docker-compose based deployment

* Create a file "my.docker.env" in the project root and add to it `BotToken="...my bot token..."`.
* Run e.g. `docker-compose up -d --build`

## General TO DO

There are lots of TODO comments in the codebase. Here I'm going to try to write my list for MVP for beginning the campaign. Where system specific I can support 13th Age only and leave Dragonbane until later.

* Monster stats. Save to the account with a name, like characters -- they can in fact go in the characters collection, with a much-reduced set of properties. For 13th Age, just the following will do: AC, PD, MD, HP. Extend the game system to declare monster properties separately, and upon character creation, use a flag to declare a character to be a monster?
* Adding monsters to encounters. We'll copy the monster directly into the encounter, rather than copying it into the adventure. Unique name generation (prefix + incrementing number.)
* Removing monsters from encounters.
* `prev` and `swap` commands for encounters, for moving to the previous initiative and for swapping two combatants around in the initiative.
* List of tags at the combatant level. Arbitrary strings, unique (case insensitive). Commands to add and remove. Use for applying conditions to player characters and monsters.
* Custom counters (optionally with variable) added on characters -- variables tracked on PC, like normal counter variables.
* Extra built-in counters per 13th Age class representing class-specific resources.
* `pc-rest` command as a helper for various kinds of rests (define in a system specific manner).

## 13th Age

### Properties (dropdown select)

These are configured in the character sheet on add or edit.

* Class (list of classes, see page 30.)
* Armor (list of armor categories: None / Light / Heavy.)

Some selections of the class property may add extra fixed counters, e.g. battle cries and spells for the bard (TODO do later after doing the basics.)

### Fixed counters

These are configured in the character sheet on add or edit.

* Level
* Strength \[STR\]
* Dexterity \[DEX\]
* Constitution \[CON\]
* Intelligence \[INT\]
* Wisdom \[WIS\]
* Charisma \[CHA\]

### Derived counters

These are automatically created by the character sub-class for the rule system. See page 31 of the rule book.

Should I allow for ad hoc modifier counters for all of these? To account for magic items, feats etc. I could also present that easily in the character sheet add/edit.

* HitPoints \[HP\]
* ArmorClass \[AC\]
* PhysicalDefense \[PD\]
* MentalDefense \[MD\]
* Initiative \[INIT\]
* Recoveries
* RecoveryDie

## Dragonbane

### Properties

* Kin (needed, because it affects e.g. movement. See page. 9.)
* Profession (like "class" in 13th Age, D&D, see page 15.)

### Fixed counters

* Strength \[STR\]
* Constitution \[CON\]
* Agility \[AGI\]
* Intelligence \[INT\]
* Willpower \[WIL\]
* Charisma \[CHA\]
* Points for all 30 core skills (see page 30 and onwards.)
* Points for the magical secondary skills.
* ArmorRating (configured as a single number rather than selecting armor types, which would be messy.)

### Derived counters

* Movement (page 27.)
* HitPoints \[HP\]
* WillpowerPoints \[WP\]

I think it's reasonable to make players make their own custom counters to track weapon durability to begin with...

TO DO come up with something that will let the GM ensure fair skill advancement between player characters (?)

### Extra variables

* Initiative
* DeathSuccesses (see page 50)
* DeathFails
* Exhausted, Sickly, Dazed, Angry, Scared, Disheartened (these are 0 or 1 only and default to 0)
* Advancement Marks for every skill (0 or 1, see page 29)

### Rests (see page 54)

* Round: recover 1d6 WP
* Stretch: recover 1d6 HP, 1d6 WP, heal a chosen condition (needs UI...)
* Shift: recover all HP, all WP, lose all conditions
