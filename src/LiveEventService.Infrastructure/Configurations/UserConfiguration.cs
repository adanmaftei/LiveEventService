using UserEntity = LiveEventService.Core.Users.User.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveEventService.Infrastructure.Users;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.IdentityId)
            .IsRequired()
            .HasMaxLength(450); // Standard size for user IDs in ASP.NET Identity
            
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);
            
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
            .OnDelete(DeleteBehavior.Restrict);
            
        // Note: OrganizedEvents relationship is not configured via EF
        // because Event.OrganizerId is a string (external identity) 
        // and User.Id is a Guid (internal ID)
    }
}
