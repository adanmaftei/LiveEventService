using EventEntity = LiveEventService.Core.Events.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveEventService.Infrastructure.Events;

public class EventConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(4000);
            
        builder.Property(e => e.StartDate)
            .IsRequired();
            
        builder.Property(e => e.EndDate)
            .IsRequired();
            
        builder.Property(e => e.Capacity)
            .IsRequired()
            .HasDefaultValue(100);
            
        builder.Property(e => e.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(e => e.TimeZone)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(e => e.OrganizerId)
            .IsRequired()
            .HasMaxLength(450); // Standard size for user IDs in ASP.NET Identity
            
        // Soft delete query filter
        // builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Relationships
        builder.HasMany(e => e.Registrations)
            .WithOne(er => er.Event)
            .HasForeignKey(er => er.EventId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Indexes
        builder.HasIndex(e => e.StartDate);
        builder.HasIndex(e => e.IsPublished);
        builder.HasIndex(e => e.OrganizerId);
        // Composite indexes to support common filtered sorts
        builder.HasIndex(e => new { e.IsPublished, e.StartDate });
        builder.HasIndex(e => new { e.OrganizerId, e.StartDate });
    }
}
