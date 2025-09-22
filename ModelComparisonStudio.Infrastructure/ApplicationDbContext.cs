using Microsoft.EntityFrameworkCore;
using ModelComparisonStudio.Core.Entities;

namespace ModelComparisonStudio.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly string _databasePath;

    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    public ApplicationDbContext(string databasePath = "evaluations.db")
    {
        _databasePath = databasePath;
    }

    /// <summary>
    /// Gets or sets the evaluations DbSet.
    /// </summary>
    public DbSet<Evaluation> Evaluations { get; set; } = null!;

    /// <summary>
    /// Configures the database connection and entity mappings.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }
    }

    /// <summary>
    /// Configures entity relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Evaluation entity
        modelBuilder.Entity<Evaluation>(entity =>
        {
            // Set primary key
            entity.HasKey(e => e.Id);

            // Configure properties
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => ModelComparisonStudio.Core.ValueObjects.EvaluationId.FromString(value))
                .ValueGeneratedNever();

            entity.Property(e => e.PromptId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PromptText)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Rating)
                .HasDefaultValue(null);

            entity.Property(e => e.Comment)
                .HasConversion(
                    comment => comment.ToString(),
                    value => ModelComparisonStudio.Core.ValueObjects.CommentText.Create(value))
                .HasMaxLength(500);

            entity.Property(e => e.ResponseTimeMs)
                .IsRequired()
                .HasDefaultValue(1000L);

            entity.Property(e => e.TokenCount)
                .HasDefaultValue(null);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.Property(e => e.IsSaved)
                .IsRequired()
                .HasDefaultValue(false);

            // Add indexes for better query performance
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ModelId, e.CreatedAt });
            entity.HasIndex(e => e.PromptId);
        });
    }
}