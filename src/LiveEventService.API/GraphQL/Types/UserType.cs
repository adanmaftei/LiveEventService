using LiveEventService.Application.Features.Users.User;

namespace LiveEventService.API.Users;

public class UserType : ObjectType<UserDto>
{
    protected override void Configure(IObjectTypeDescriptor<UserDto> descriptor)
    {
        descriptor.Description("Represents a user in the system");
        
        descriptor
            .Field(u => u.Id)
            .Description("The unique identifier of the user");
            
        descriptor
            .Field(u => u.IdentityId)
            .Description("The identity provider's ID for the user");
            
        descriptor
            .Field(u => u.CreatedAt)
            .Description("The date and time when the user was created");
            
        descriptor
            .Field(u => u.UpdatedAt)
            .Description("The date and time when the user was last updated");
    }
}
