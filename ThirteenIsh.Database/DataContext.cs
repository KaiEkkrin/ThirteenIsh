using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ThirteenIsh.Database.Entities;
using ThirteenIsh.Database.Entities.Combatants;
using ThirteenIsh.Database.Entities.Messages;

namespace ThirteenIsh.Database;

public class DataContext : DbContext
{
    public DbSet<Adventure> Adventures { get; set; }
    public DbSet<Adventurer> Adventurers { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Encounter> Encounters { get; set; }
    public DbSet<Guild> Guilds { get; set; }

    // Message types as TPH mapping: https://learn.microsoft.com/en-us/ef/core/modeling/inheritance
    public DbSet<MessageBase> Messages { get; set; }
    public DbSet<AdventureMessageBase> AdventureMessages { get; set; }
    public DbSet<EncounterMessageBase> EncounterMessages { get; set; }
    public DbSet<AddCharacterMessage> AddCharacterMessages { get; set; }
    public DbSet<DeleteAdventureMessage> DeleteAdventureMessages { get; set; }
    public DbSet<DeleteCharacterMessage> DeleteCharacterMessages { get; set; }
    public DbSet<DeleteCustomCounterMessage> DeleteCustomCounterMessages { get; set; }
    public DbSet<EncounterDamageMessage> EncounterDamageMessages { get; set; }
    public DbSet<EndEncounterMessage> EndEncounterMessages { get; set; }
    public DbSet<LeaveAdventureMessage> LeaveAdventureMessages { get; set; }
    public DbSet<ListCharactersMessage> ListCharactersMessages { get; set; }
    public DbSet<ResetAdventurerMessage> ResetAdventurerMessages { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        // See https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/events
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adventurer>()
            .OwnsOne(c => c.Sheet, s =>
            {
                s.ToJson();
                s.OwnsMany(s => s.Counters);
                s.OwnsMany(s => s.Properties);
                s.OwnsMany(s => s.CustomCounters);
            })
            .OwnsOne(c => c.Fixes, f =>
            {
                f.ToJson();
                f.OwnsMany(f => f.Counters);
            })
            .OwnsOne(c => c.Variables, v =>
            {
                v.ToJson();
                v.OwnsMany(v => v.Counters);
            });

        modelBuilder.Entity<Character>()
            .OwnsOne(c => c.Sheet, s =>
            {
                s.ToJson();
                s.OwnsMany(s => s.Counters);
                s.OwnsMany(s => s.Properties);
                s.OwnsMany(s => s.CustomCounters);
            });

        modelBuilder.Entity<Encounter>()
            .OwnsOne(c => c.State, s =>
            {
                s.ToJson();
                s.OwnsMany(s => s.Adventurers);
                s.OwnsMany(s => s.Monsters, m =>
                {
                    m.OwnsOne(m => m.Sheet, h =>
                    {
                        h.OwnsMany(h => h.Counters);
                        h.OwnsMany(h => h.Properties);
                        h.OwnsMany(h => h.CustomCounters);
                    });
                    m.OwnsOne(m => m.Fixes, f =>
                    {
                        f.OwnsMany(f => f.Counters);
                    });
                    m.OwnsOne(m => m.Variables, v =>
                    {
                        v.OwnsMany(v => v.Counters);
                    });
                });
                s.OwnsMany(s => s.Counters);
            });

        base.OnModelCreating(modelBuilder);
    }

    private void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.Entity is IHasLastEdited lastEditedEntity)
        {
            if (e.Entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                lastEditedEntity.LastEdited = DateTimeOffset.UtcNow;
            }
        }
    }

    /// <summary>
    /// Gets the character associated with a combatant.
    /// </summary>
    public async Task<ITrackedCharacter?> GetCharacterAsync(
        CombatantBase combatant,
        Encounter encounter,
        CancellationToken cancellationToken = default)
    {
        return combatant switch
        {
            AdventurerCombatant adventurerCombatant => await Adventurers.SingleOrDefaultAsync(
                a => a.Adventure.GuildId == encounter.GuildId &&
                     a.Adventure.Name == encounter.AdventureName &&
                     a.Name == adventurerCombatant.Name,
                cancellationToken),
            MonsterCombatant monsterCombatant => monsterCombatant,
            _ => throw new ArgumentException($"Unknown combatant type: {combatant.GetType().Name}", nameof(combatant))
        };
    }
}
