using LiveEventService.Core.Users.User;
using MediatR;
using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;

namespace LiveEventService.Application.Features.Users.User.Get;

public class GetUserQuery : IRequest<BaseResponse<UserDto>>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, BaseResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserQueryHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

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
