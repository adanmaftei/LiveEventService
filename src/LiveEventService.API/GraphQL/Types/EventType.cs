using LiveEventService.API.GraphQL.DataLoaders;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Core.Users.User;

namespace LiveEventService.API.GraphQL.Types;

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
            .Description("The start date and time of the event");

        descriptor
            .Field(e => e.EndDateTime)
            .Description("The end date and time of the event");

        descriptor
            .Field(e => e.Location)
            .Description("The location of the event");

        descriptor
            .Field(e => e.Capacity)
            .Description("The maximum capacity of the event");

        descriptor
            .Field(e => e.IsPublished)
            .Description("Whether the event is published and visible to participants");

        descriptor
            .Field(e => e.CreatedAt)
            .Description("The date and time when the event was created");

        descriptor
            .Field(e => e.UpdatedAt)
            .Description("The date and time when the event was last updated");

        // Organizer name resolved via DataLoader to batch across selection sets
        descriptor
            .Field("organizerName")
            .Type<StringType>()
            .Description("The organizer display name")
            .Resolve(async (ctx, ct) =>
            {
                var evt = ctx.Parent<EventDto>();
                if (string.IsNullOrWhiteSpace(evt.OrganizerId))
                {
                    return string.Empty;
                }
                var loader = ctx.DataLoader<UserByIdentityIdDataLoader>();
                var user = await loader.LoadAsync(evt.OrganizerId, ct);
                if (user == null)
                {
                    return string.Empty;
                }
                return $"{user.FirstName} {user.LastName}".Trim();
            });
    }
}
