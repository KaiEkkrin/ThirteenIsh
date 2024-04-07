using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ThirteenIsh.Database.Entities;

namespace ThirteenIsh.Database;

public class DataContext : DbContext
{
    public DbSet<Character> Characters { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        // See https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/events
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>()
            .OwnsOne(c => c.Sheet, s =>
            {
                s.ToJson();
                s.OwnsMany(s => s.Counters);
                s.OwnsMany(s => s.Properties);
            });

        base.OnModelCreating(modelBuilder);
    }

    private void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.Entity is IHasLastEdited lastEditedEntity)
        {
            if (e.Entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                lastEditedEntity.LastEdited = DateTimeOffset.Now;
            }
        }
    }
}
