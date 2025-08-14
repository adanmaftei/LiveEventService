using System.Text.Json;
using LiveEventService.Application.Common.Models;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Users.Queries.ExportUserData;

public class ExportUserDataQueryHandler : IQueryHandler<ExportUserDataQuery, BaseResponse<ExportUserDataResult>>
{
    private readonly IUserRepository _userRepository;

    public ExportUserDataQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<BaseResponse<ExportUserDataResult>> Handle(ExportUserDataQuery request, CancellationToken cancellationToken)
    {
        // Support both identity-id (string) and internal GUID id
        var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            if (Guid.TryParse(request.UserId, out var userGuid))
            {
                user = await _userRepository.GetByIdAsync(userGuid, cancellationToken);
            }
        }
        if (user == null)
        {
            return BaseResponse<ExportUserDataResult>.Failed("User not found");
        }

        // Load registrations via navigation if needed (simple projection here)
        var export = new
        {
            user.Id,
            user.IdentityId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.IsActive,
            Registrations = user.EventRegistrations.Select(r => new
            {
                r.Id,
                r.EventId,
                r.RegistrationDate,
                r.Status,
                r.PositionInQueue,
                r.Notes
            })
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,            
        });

        return BaseResponse<ExportUserDataResult>.Succeeded(new ExportUserDataResult { Json = json });
    }
}


