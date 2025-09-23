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
    /// Gets or sets the prompt templates DbSet.
    /// </summary>
    public DbSet<PromptTemplate> PromptTemplates { get; set; } = null!;

    /// <summary>
    /// Gets or sets the prompt categories DbSet.
    /// </summary>
    public DbSet<PromptCategory> PromptCategories { get; set; } = null!;

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

        // Ignore TemplateVariable as it's stored as JSON within PromptTemplate
        modelBuilder.Ignore<ModelComparisonStudio.Core.Entities.TemplateVariable>();

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

        // Configure PromptTemplate entity
        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            // Set primary key
            entity.HasKey(t => t.Id);

            // Configure properties
            entity.Property(t => t.Id)
                .ValueGeneratedNever(); // We'll handle ID generation ourselves

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.Content)
                .IsRequired();

            entity.Property(t => t.Description)
                .HasMaxLength(200);

            entity.Property(t => t.VariablesJson)
                .HasConversion(
                    v => v ?? "[]",
                    v => v ?? "[]")
                .HasColumnType("TEXT");

            entity.Property(t => t.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.IsFavorite)
                .HasDefaultValue(false);

            entity.Property(t => t.UsageCount)
                .HasDefaultValue(0);

            entity.Property(t => t.IsSystemTemplate)
                .HasDefaultValue(false);

            entity.Property(t => t.CreatedAt)
                .IsRequired();

            entity.Property(t => t.UpdatedAt)
                .IsRequired();

            entity.Property(t => t.LastUsedAt)
                .IsRequired(false);

            // Add indexes for better query performance
            entity.HasIndex(t => t.Category);
            entity.HasIndex(t => t.IsSystemTemplate);
            entity.HasIndex(t => t.IsFavorite);
            entity.HasIndex(t => t.UsageCount);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.UpdatedAt);
            entity.HasIndex(t => new { t.Category, t.IsFavorite });
            entity.HasIndex(t => new { t.IsSystemTemplate, t.IsFavorite });
        });

        // Configure PromptCategory entity
        modelBuilder.Entity<PromptCategory>(entity =>
        {
            // Set primary key
            entity.HasKey(c => c.Id);

            // Configure properties
            entity.Property(c => c.Id)
                .ValueGeneratedNever(); // We'll handle ID generation ourselves

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(c => c.Description)
                .HasMaxLength(200);

            entity.Property(c => c.Color)
                .HasMaxLength(7);

            entity.Property(c => c.CreatedAt)
                .IsRequired();

            entity.Property(c => c.TemplateCount)
                .HasDefaultValue(0);

            // Add unique constraint for name
            entity.HasIndex(c => c.Name)
                .IsUnique();

            // Add index for better query performance
            entity.HasIndex(c => c.CreatedAt);
        });
    }
}