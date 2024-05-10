using BlazorSbt.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorSbt.Shared.Data;

public class DivisionContext : DbContext
{
    public DivisionContext(DbContextOptions<DivisionContext> options)
        : base(options)
    {
    }

    public DbSet<Division> Divisions { get; set; } = default!;
    //public DbSet<BlazorSbt.Shared.Schedules> Schedules { get; set; } = default!;
    //public DbSet<BlazorSbt.Shared.Standings> Standings { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Division>()
            .HasKey(d => new { d.Organization, d.Abbreviation });

        modelBuilder.Entity<Standings>()
            .HasKey(s => new { s.Organization, s.Abbreviation, s.TeamID });
        modelBuilder.Entity<Schedule>()
            .HasKey(s => new { s.Organization, s.Abbreviation, s.GameID });

        modelBuilder.Entity<Division>().ToTable("SbtMultiDB_Division");
        modelBuilder.Entity<Standings>().ToTable("SbtMultiDB_Standings");
        modelBuilder.Entity<Schedule>().ToTable("SbtMultiDB_Schedule");
    }

    protected async Task<int> SaveChangesSqlCommonAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.HasChanges())
        {
            var divisionsToDelete = new List<Division>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Deleted && entry.Entity is Division division)
                {
                    divisionsToDelete.Add(division);
                }
            }

            foreach (var division in divisionsToDelete)
            {
                // Manually delete related Standings and Schedule.
                var relatedStandings = Set<Standings>().Where(s => s.Organization == division.Organization && s.Abbreviation == division.Abbreviation);
                var relatedSchedule = Set<Schedule>().Where(s => s.Organization == division.Organization && s.Abbreviation == division.Abbreviation);

                Set<Standings>().RemoveRange(relatedStandings);
                Set<Schedule>().RemoveRange(relatedSchedule);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        return 0;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await this.SaveChangesSqlCommonAsync(cancellationToken);
    }
}
