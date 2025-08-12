using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveEventService.Infrastructure.Registrations;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistrationEntity>
{
    public void Configure(EntityTypeBuilder<EventRegistrationEntity> builder)
    {
        builder.ToTable("EventRegistrations");
        
        builder.HasKey(er => er.Id);
        
        builder.Property(er => er.RegistrationDate)
            .IsRequired();
            
        builder.Property(er => er.Status)
            .IsRequired()
            .HasConversion<int>(); // Use int instead of string conversion
            
        builder.Property(er => er.Notes)
            .HasMaxLength(1000);
            
        builder.Property(er => er.PositionInQueue);
        
        // Relationships
        builder.HasOne(er => er.Event)
            .WithMany(e => e.Registrations)
            .HasForeignKey(er => er.EventId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(er => er.User)
            .WithMany(u => u.EventRegistrations)
            .HasForeignKey(er => er.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Basic indexes only (remove unique constraint temporarily)
        builder.HasIndex(er => er.EventId);
        builder.HasIndex(er => er.UserId);
        builder.HasIndex(er => er.Status);
    }
}
