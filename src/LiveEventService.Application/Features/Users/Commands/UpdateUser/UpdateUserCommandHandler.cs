using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Users.User;

namespace LiveEventService.Application.Features.Users.User.Update;

/// <summary>
/// Handles updating user profile information and returns the updated DTO.
/// </summary>
public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, BaseResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The repository used to access and manage user data.</param>
    /// <param name="mapper">The mapper used for mapping domain entities to DTOs.</param>
    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Get the user by identity ID
        var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return BaseResponse<UserDto>.Failed("User not found");
        }

        // Update user properties
        user.UpdateProfile(
            request.User.FirstName,
            request.User.LastName,
            request.User.PhoneNumber ?? string.Empty);

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Map to DTO
        var userDto = _mapper.Map<UserDto>(user);

        return BaseResponse<UserDto>.Succeeded(userDto, "User updated successfully");
    }
}
