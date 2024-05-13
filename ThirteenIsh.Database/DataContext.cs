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

    // Combatant types as TPH mapping: https://learn.microsoft.com/en-us/ef/core/modeling/inheritance
    public DbSet<CombatantBase> Combatants { get; set; }
    public DbSet<AdventurerCombatant> AdventurerCombatants { get; set; }
    public DbSet<MonsterCombatant> MonsterCombatants { get; set; }

    // Message types as TPH mapping: https://learn.microsoft.com/en-us/ef/core/modeling/inheritance
    public DbSet<MessageBase> Messages { get; set; }
    public DbSet<AdventureMessageBase> AdventureMessages { get; set; }
    public DbSet<EncounterMessageBase> EncounterMessages { get; set; }
    public DbSet<AddCharacterMessage> AddCharacterMessages { get; set; }
    public DbSet<DeleteAdventureMessage> DeleteAdventureMessages { get; set; }
    public DbSet<DeleteCharacterMessage> DeleteCharacterMessages { get; set; }
    public DbSet<EncounterDamageMessage> EncounterDamageMessages { get; set; }
    public DbSet<EndEncounterMessage> EndEncounterMessages { get; set; }
    public DbSet<LeaveAdventureMessage> LeaveAdventureMessages { get; set; }
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
            });

        modelBuilder.Entity<CombatantBase>()
            .HasOne(c => c.Encounter)
            .WithMany(e => e.Combatants)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Encounter>()
            .OwnsOne(c => c.Variables, v =>
            {
                v.ToJson();
                v.OwnsMany(v => v.Counters);
            });

        modelBuilder.Entity<MonsterCombatant>()
            .OwnsOne(c => c.Sheet, s =>
            {
                s.ToJson();
                s.OwnsMany(s => s.Counters);
                s.OwnsMany(s => s.Properties);
            })
            .OwnsOne(c => c.Variables, v =>
            {
                v.ToJson();
                v.OwnsMany(v => v.Counters);
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
}
