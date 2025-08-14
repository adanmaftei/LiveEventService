using LiveEventService.Application.Common.Models;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Users.User.Erase;

public class EraseUserCommandHandler : ICommandHandler<EraseUserCommand, BaseResponse<bool>>
{
    private readonly IUserRepository _userRepository;

    public EraseUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<BaseResponse<bool>> Handle(EraseUserCommand request, CancellationToken cancellationToken)
    {
        // Support both identity-id (string) and internal GUID id
        var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
        if (user == null && Guid.TryParse(request.UserId, out var userGuid))
        {
            user = await _userRepository.GetByIdAsync(userGuid, cancellationToken);
        }
        if (user == null)
        {
            return BaseResponse<bool>.Failed("User not found");
        }

        if (request.HardDelete)
        {
            await _userRepository.DeleteAsync(user, cancellationToken);
            return BaseResponse<bool>.Succeeded(true);
        }

        // Soft approach: anonymize and deactivate account
        user.DeactivateAndAnonymize($"anon+{user.Id}@example.invalid");
        await _userRepository.UpdateAsync(user, cancellationToken);

        return BaseResponse<bool>.Succeeded(true);
    }
}


