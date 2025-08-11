using AutoMapper;
using LiveEventService.Core.Users.User;
using MediatR;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.User.Create;

public class CreateUserCommand : IRequest<BaseResponse<UserDto>>
{
    public CreateUserDto User { get; set; } = null!;
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, BaseResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

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
