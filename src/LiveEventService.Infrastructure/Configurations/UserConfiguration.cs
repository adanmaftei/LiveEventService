using UserEntity = LiveEventService.Core.Users.User.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveEventService.Infrastructure.Users;

/// <summary>
/// EF Core configuration for the <see cref="UserEntity"/> aggregate.
/// Applies constraints, indexes, and relationships.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    /// <summary>
    /// Configures the <see cref="UserEntity"/> model.
    /// </summary>
    /// <param name="builder">The entity type builder for configuring the model.</param>
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.IdentityId)
            .IsRequired()
            .HasMaxLength(450); // Standard size for user IDs in ASP.NET Identity

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(512); // allow for encrypted content length

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(512); // allow for encrypted content length

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(512); // allow for encrypted content length

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(512); // allow for encrypted content length

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(u => u.IdentityId)
            .IsUnique();

        builder.HasIndex(u => u.Email);

        // Relationships
        builder.HasMany(u => u.EventRegistrations)
            .WithOne(er => er.User)
            .HasForeignKey(er => er.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: OrganizedEvents relationship is not configured via EF
        // because Event.OrganizerId is a string (external identity)
        // and User.Id is a Guid (internal ID)
    }
}
