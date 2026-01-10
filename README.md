# ThirteenIsh

An experimental Discord bot for dice rolling and TTRPG tracking

## Development with VS Code Dev Container

This project includes a VS Code dev container setup for easy development. The dev container provides:

- .NET 10 SDK with helpful tools pre-installed:
  - `dotnet ef` - Entity Framework Core migrations tool
  - `dotnet-outdated` - Check for outdated NuGet packages
  - `dotnet-consolidate` - Verify package version consistency
- Node.js LTS
- PostgreSQL database (automatically configured with default credentials)
- All necessary VS Code extensions

### Setting Up the Dev Container

1. **Install Prerequisites:**
   - Ensure you have Docker and the "Dev Containers" VS Code extension installed

2. **Configure Your Bot Token (Secret):**

   **IMPORTANT:** Your Discord bot token is a secret and must never be committed to the repository.

   Set your bot token as an environment variable on your local machine:

   **On Windows (PowerShell):**
   ```powershell
   $env:BOT_TOKEN = "your-actual-bot-token-here"
   ```

   **On Windows (Command Prompt):**
   ```cmd
   set BOT_TOKEN=your-actual-bot-token-here
   ```

   **On macOS/Linux:**
   ```bash
   export BOT_TOKEN="your-actual-bot-token-here"
   ```

   **For Permanent Setup:**
   - **Windows:** Add the variable to your system environment variables via System Properties → Environment Variables
   - **macOS/Linux:** Add the export line to your `~/.bashrc`, `~/.zshrc`, or equivalent shell profile file

3. **Start the Dev Container:**

   **Recommended Method (Better Performance):**
   - Use Command Palette (Ctrl/Cmd+Shift+P) → "Dev Containers: Clone Repository in Container Volume"
   - Enter the repository URL: `https://github.com/KaiEkkrin/ThirteenIsh.git`
   - This clones the repository directly into a Docker volume for optimal performance

   **Alternative Method:**
   - Open the project in VS Code locally first
   - When prompted, click "Reopen in Container" or use Command Palette → "Dev Containers: Reopen in Container"

   The container will build and start automatically, including the PostgreSQL database. Your `BOT_TOKEN` will be securely injected from your local environment.

The dev container automatically configures a PostgreSQL database with these credentials:
- Username: `thirteenish`
- Password: `devcontainerpassword`
- Database: `thirteenish`

**Note:** The PostgreSQL password is hardcoded for development convenience since it's only accessible within the local container.

## Setup

I followed the instructions in [Discord.net -- Your First Bot](https://discordnet.dev/guides/getting_started/first-bot.html) and in [Discord.net -- Getting started with application commands](https://discordnet.dev/guides/int_basics/application-commands/intro.html). The bot will need to be added to servers with the following permissions:

* bot
* applications.commands

And, it will need these bot permissions:

* Manage Messages
* Pin Messages

## Database

ThirteenIsh uses Postgres. You can run up a basic Docker container:

```powershell
docker run --name thirteenish-postgres --restart unless-stopped -d -e POSTGRES_USER=thirteenish -e POSTGRES_PASSWORD=<...password...> -p 5432:5432 postgres:16
```

## Configuration settings

Configure these via command line, environment variables or user secrets.

* "BotToken": the Discord bot token.
* "DbConnectionString": the Postgres connection string, e.g. "Host=localhost;Database=thirteenish;Username=thirteenish;Password=<...password...>"

## Docker-compose based deployment

**Note:** The dev container setup above is recommended for development. For production deployment, you'll need to configure environment variables appropriately for your deployment environment.

For local testing with docker-compose, you can use the main `docker-compose.yaml` file, but you'll need to set the required environment variables in your deployment environment:
* `BotToken` - your Discord bot token
* `DbConnectionString` - your database connection string
* `POSTGRES_USER` - your PostgreSQL username
* `POSTGRES_PASSWORD` - your PostgreSQL password

## If you make a change to the data model (ThirteenIsh.Database)

You'll need the `dotnet ef` tool installed:

```powershell
dotnet tool install --global dotnet-ef
```

Add a new migration:

```powershell
cd ThirteenIsh.Database
dotnet ef migrations add <...migration name...> --startup-project ..\ThirteenIsh\ThirteenIsh.csproj
```

This will scaffold the migration in code but won't change the database. ThirteenIsh updates the database itself when it starts up, you don't need to call `dotnet ef database update`.

There are other useful commands, e.g. you can list the existing migrations with

```powershell
dotnet ef migrations list --startup-project ..\ThirteenIsh\ThirteenIsh.csproj
```

## Package Management

The dev container automatically installs helpful tools for managing NuGet packages. These tools help maintain package versions, check for security vulnerabilities, and ensure consistency across projects.

### Checking for Package Issues

**Check for vulnerable packages:**
```bash
dotnet list package --vulnerable
```

**Check for outdated packages:**
```bash
dotnet list package --outdated
```

**View all packages including transitive dependencies:**
```bash
dotnet list package --include-transitive
```

### Automated Package Management Tools

The following tools are pre-installed in the dev container:

#### dotnet-outdated
Identifies and optionally updates outdated NuGet packages:

```bash
# Check for outdated packages
dotnet outdated

# Automatically upgrade packages (review breaking changes first!)
dotnet outdated --upgrade
```

**Note:** Use `--upgrade` cautiously, especially for major version bumps that may include breaking changes.

#### dotnet-consolidate
Verifies that package versions are consistent across all projects in the solution:

```bash
# Check all packages are consolidated
dotnet consolidate

# Check a specific package
dotnet consolidate --package-id Microsoft.EntityFrameworkCore.Design
```

This tool exits with code 0 if all packages are consolidated, or prints discrepancies if version conflicts are found.

### NuGet Warning Codes

When running `dotnet restore` or `dotnet build`, watch for these warning codes:

- **NU1605**: Package downgrade detected - a project is requesting an older version than already resolved
- **NU1107**: Version conflict detected - NuGet found a conflict it couldn't resolve automatically
- **NU1608**: Dependency constraint mismatch - package version constraint is incompatible with resolved version

### Manual Installation (if not using dev container)

If you're not using the dev container, install these tools manually:

```bash
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-outdated-tool
dotnet tool install --global dotnet-consolidate
```

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
