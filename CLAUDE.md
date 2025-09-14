# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ThirteenIsh is a Discord bot written in C# (.NET 8) for tabletop RPG (TTRPG) dice rolling and campaign tracking. It supports multiple game systems including 13th Age, Dragonbane, and Stars Without Number.

## Common Development Commands

### Building and Running
- `dotnet build` - Build the entire solution
- `dotnet run --project ThirteenIsh` - Run the Discord bot
- `dotnet test` - Run all tests
- `dotnet test --project ThirteenIsh.Tests` - Run specific test project

### Database Operations
The project uses Entity Framework Core with PostgreSQL:
- `dotnet ef migrations add <migration-name> --startup-project ThirteenIsh --project ThirteenIsh.Database` - Add new migration
- `dotnet ef migrations list --startup-project ThirteenIsh --project ThirteenIsh.Database` - List existing migrations
- Database updates are handled automatically by the application on startup

### Development Environment
The project includes a VS Code dev container setup with PostgreSQL. Bot requires a `BOT_TOKEN` environment variable for Discord integration.

## Architecture

### Project Structure
- **ThirteenIsh** - Main Discord bot application (Worker service)
- **ThirteenIsh.Database** - Entity Framework data layer with PostgreSQL
- **ThirteenIsh.Tests** - Unit tests using xUnit and Shouldly

### Key Components

#### Game Systems
The bot supports multiple TTRPG systems through an extensible architecture:
- Base classes: `GameSystem` and `CharacterSystem` in `/Game/`
- Specific systems: `/Game/ThirteenthAge/`, `/Game/Dragonbane/`, `/Game/Swn/`
- Each system defines character properties, counters, and game-specific rules

#### Database Architecture
- Entity base classes provide common functionality (`EntityBase`, `SearchableNamedEntityBase`)
- Core entities: `Guild`, `Adventure`, `Character`, `Encounter`, `Adventurer`
- Message-based operations for tracking changes
- Combatants system for encounter management

#### Discord Integration
- Commands in `/Commands/` directory using Discord.Net slash commands
- Command registration handled in `CommandRegistration.cs`
- Channel-specific guild messages for persistence

### Key Patterns
- Game systems are pluggable via abstract base classes
- Database operations use Entity Framework with automatic migrations
- Discord commands follow a base class hierarchy for consistent implementation
- Character properties support both fixed and custom counters
- Encounter system supports initiative tracking and combatant management

## Configuration
- Bot token via environment variable `BOT_TOKEN`
- Database connection string configurable via `DbConnectionString`
- User secrets supported for development
- Docker deployment supported with docker-compose

## Testing
Tests use xUnit framework with Shouldly assertions. Test doubles include mock implementations for random number generation and other external dependencies.