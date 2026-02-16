using Microsoft.EntityFrameworkCore;
using myapp.Data.Entities;

namespace myapp.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<GameState> GameState { get; set; } = null!;
    public DbSet<BuildingEntity> Buildings { get; set; } = null!;
    public DbSet<TechnologyEntity> Technologies { get; set; } = null!;
    public DbSet<ShipEntity> Ships { get; set; } = null!;
    public DbSet<DefenseEntity> Defenses { get; set; } = null!;
    public DbSet<BuildingQueueEntity> BuildingQueue { get; set; } = null!;
    public DbSet<ShipyardQueueEntity> ShipyardQueue { get; set; } = null!;
    public DbSet<DefenseQueueEntity> DefenseQueue { get; set; } = null!;
    public DbSet<ResearchQueueEntity> ResearchQueue { get; set; } = null!;
    public DbSet<FleetMissionEntity> FleetMissions { get; set; } = null!;
    public DbSet<FleetMissionShipEntity> FleetMissionShips { get; set; } = null!;
    public DbSet<GameMessageEntity> Messages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // GameState - single row
        modelBuilder.Entity<GameState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        // Unique constraints
        modelBuilder.Entity<BuildingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BuildingType).IsUnique();
        });

        modelBuilder.Entity<TechnologyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TechnologyType).IsUnique();
        });

        modelBuilder.Entity<ShipEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShipType).IsUnique();
        });

        modelBuilder.Entity<DefenseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DefenseType).IsUnique();
        });

        modelBuilder.Entity<BuildingQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ShipyardQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<DefenseQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ResearchQueueEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FleetMissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FleetMissionShipEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.FleetMission)
                .WithMany()
                .HasForeignKey(e => e.FleetMissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GameMessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
