using Microsoft.EntityFrameworkCore;
using PhotoService.Models;

namespace PhotoService.Data;

/// <summary>
/// Entity Framework Database Context for Photo Service
/// Standard EF Core setup with MySQL configuration and photo management
/// </summary>
public class PhotoContext : DbContext
{
    /// <summary>
    /// Constructor accepting DbContext options
    /// Standard dependency injection pattern for EF Core
    /// </summary>
    /// <param name="options">Database context configuration options</param>
    public PhotoContext(DbContextOptions<PhotoContext> options) : base(options)
    {
    }

    /// <summary>
    /// Photos table - Main entity for photo storage and metadata
    /// </summary>
    public DbSet<Photo> Photos { get; set; }

    /// <summary>
    /// Model configuration and database schema setup
    /// Configures indexes, constraints, and relationships
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder for schema configuration</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================================
        // PHOTO ENTITY CONFIGURATION
        // Indexes and constraints for optimal query performance
        // ================================

        modelBuilder.Entity<Photo>(entity =>
        {
            // Primary key configuration (already defined with [Key] attribute)
            entity.HasKey(e => e.Id);

            // Composite index for user photos queries
            // Most common query: get photos by user, ordered by display order
            entity.HasIndex(e => new { e.UserId, e.DisplayOrder, e.IsDeleted })
                  .HasDatabaseName("IX_Photos_User_DisplayOrder_Deleted");

            // Index for primary photo lookup
            // Quick access to user's primary profile photo
            entity.HasIndex(e => new { e.UserId, e.IsPrimary, e.IsDeleted })
                  .HasDatabaseName("IX_Photos_User_Primary_Deleted");

            // Index for moderation workflow
            // Content moderation team queries
            entity.HasIndex(e => new { e.ModerationStatus, e.CreatedAt })
                  .HasDatabaseName("IX_Photos_Moderation_Created");

            // Index for cleanup operations
            // Periodic cleanup of deleted photos
            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt })
                  .HasDatabaseName("IX_Photos_Deleted_DeletedAt");

            // Unique constraint for stored filenames
            // Prevents file naming conflicts
            entity.HasIndex(e => e.StoredFileName)
                  .IsUnique()
                  .HasDatabaseName("IX_Photos_StoredFileName_Unique");

            // ================================
            // COLUMN CONFIGURATIONS
            // Specific column constraints and defaults
            // ================================

            // UserId configuration
            entity.Property(e => e.UserId)
                  .IsRequired()
                  .HasComment("Foreign key to User entity in auth service");

            // String length constraints (already defined with MaxLength attributes)
            entity.Property(e => e.OriginalFileName)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.StoredFileName)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.FileExtension)
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.ModerationStatus)
                  .HasMaxLength(20)
                  .IsRequired()
                  .HasDefaultValue(ModerationStatus.AutoApproved);

            entity.Property(e => e.ModerationNotes)
                  .HasMaxLength(500);

            // Timestamp configurations with UTC defaults
            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasDefaultValueSql("UTC_TIMESTAMP()")
                  .HasComment("Photo upload timestamp (UTC)");

            entity.Property(e => e.UpdatedAt)
                  .HasComment("Last metadata update timestamp (UTC)");

            entity.Property(e => e.DeletedAt)
                  .HasComment("Soft deletion timestamp (UTC)");

            // Boolean defaults
            entity.Property(e => e.IsPrimary)
                  .IsRequired()
                  .HasDefaultValue(false);

            entity.Property(e => e.IsDeleted)
                  .IsRequired()
                  .HasDefaultValue(false);

            // Display order default
            entity.Property(e => e.DisplayOrder)
                  .IsRequired()
                  .HasDefaultValue(1);

            // Quality score default
            entity.Property(e => e.QualityScore)
                  .IsRequired()
                  .HasDefaultValue(100);

            // ================================
            // BUSINESS RULE CONSTRAINTS
            // Database-level enforcement of business logic
            // ================================

            // Check constraint: File size must be positive
            entity.HasCheckConstraint("CK_Photos_FileSizeBytes", 
                $"FileSizeBytes > 0 AND FileSizeBytes <= {PhotoConstants.MaxFileSizeBytes}");

            // Check constraint: Image dimensions must be positive
            entity.HasCheckConstraint("CK_Photos_Dimensions", 
                "Width > 0 AND Height > 0");

            // Check constraint: Display order must be positive
            entity.HasCheckConstraint("CK_Photos_DisplayOrder", 
                "DisplayOrder > 0");

            // Check constraint: Quality score range
            entity.HasCheckConstraint("CK_Photos_QualityScore", 
                "QualityScore >= 1 AND QualityScore <= 100");

            // Check constraint: Valid moderation status
            entity.HasCheckConstraint("CK_Photos_ModerationStatus",
                "ModerationStatus IN ('AUTO_APPROVED', 'PENDING_REVIEW', 'APPROVED', 'REJECTED')");

            // Check constraint: Logical deletion consistency
            entity.HasCheckConstraint("CK_Photos_Deletion_Logic",
                "(IsDeleted = 0 AND DeletedAt IS NULL) OR (IsDeleted = 1 AND DeletedAt IS NOT NULL)");
        });

        // ================================
        // SEED DATA FOR DEVELOPMENT
        // Test data for local development environment
        // ================================
        
        // Note: Seed data would be added here for development
        // Production environments should not include test photos
    }

    /// <summary>
    /// Override SaveChanges to handle automatic timestamp updates
    /// Standard pattern for audit trail maintenance
    /// </summary>
    /// <returns>Number of affected records</returns>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to handle automatic timestamp updates
    /// Async version of SaveChanges with timestamp handling
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of affected records</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically update timestamps on entity changes
    /// Handles UpdatedAt and DeletedAt timestamp management
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Photo>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    // Update timestamp for any modification
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Convert hard delete to soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
