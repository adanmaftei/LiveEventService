using LiveEventService.Core.Users.User;
using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Users.User.Get;

/// <summary>
/// Handles fetching a user and mapping to a DTO response.
/// </summary>
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, BaseResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserQueryHandler"/> class.
    /// </summary>
    /// <param name="userRepository">The repository used to fetch user data.</param>
    /// <param name="mapper">The mapper used to map domain entities to DTOs.</param>
    public GetUserQueryHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<BaseResponse<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdentityIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return BaseResponse<UserDto>.Failed("User not found");
        }

        // Map to DTO
        var userDto = _mapper.Map<UserDto>(user);

        return BaseResponse<UserDto>.Succeeded(userDto);
    }
}
