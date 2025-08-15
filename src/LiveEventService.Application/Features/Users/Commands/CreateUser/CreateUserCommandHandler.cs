using AutoMapper;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.User.Create;

/// <summary>
/// Handles creation of users, enforcing unique email and mapping to DTO.
/// </summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, BaseResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The repository used to manage user data.</param>
    /// <param name="mapper">The mapper used to map entities to DTOs.</param>
    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email is already in use
        var existingUser = await _userRepository.GetByEmailAsync(request.User.Email, cancellationToken);
        if (existingUser != null)
        {
            return BaseResponse<UserDto>.Failed("Email is already in use");
        }

        // Create new user
        var newUser = new UserEntity(
            request.User.IdentityId,
            request.User.Email,
            request.User.FirstName,
            request.User.LastName,
            request.User.PhoneNumber ?? string.Empty);

        var createdUser = await _userRepository.AddAsync(newUser, cancellationToken);

        // Map to DTO
        var userDto = _mapper.Map<UserDto>(createdUser);

        return BaseResponse<UserDto>.Succeeded(userDto, "User created successfully");
    }
}
