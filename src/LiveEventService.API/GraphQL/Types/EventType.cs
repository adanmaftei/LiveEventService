using LiveEventService.Application.Features.Events.Event;

namespace LiveEventService.API.Events;

public class EventType : ObjectType<EventDto>
{
    protected override void Configure(IObjectTypeDescriptor<EventDto> descriptor)
    {
        descriptor.Description("Represents an event in the system");
        
        descriptor
            .Field(e => e.Id)
            .Description("The unique identifier of the event");
            
        descriptor
            .Field(e => e.Title)
            .Description("The title of the event");
            
        descriptor
            .Field(e => e.Description)
            .Description("The description of the event");
            
        descriptor
            .Field(e => e.StartDateTime)
            .Description("The start date and time of the event in UTC");
            
        descriptor
            .Field(e => e.EndDateTime)
            .Description("The end date and time of the event in UTC");
            
        descriptor
            .Field(e => e.TimeZone)
            .Description("The time zone of the event");
            
        descriptor
            .Field(e => e.Location)
            .Description("The physical location of the event");
            
        descriptor
            .Field(e => e.Address)
            .Description("The full address of the event location");
            
        descriptor
            .Field(e => e.OnlineMeetingUrl)
            .Description("The URL for online event participation");
            
        descriptor
            .Field(e => e.AvailableSpots)
            .Description("The number of available spots remaining");
            
        descriptor
            .Field(e => e.IsPublished)
            .Description("Indicates if the event is published and visible to users");
            
        descriptor
            .Field(e => e.OrganizerId)
            .Description("The ID of the user who organized the event");
            
        descriptor
            .Field(e => e.OrganizerName)
            .Description("The name of the event organizer");
    }
}
