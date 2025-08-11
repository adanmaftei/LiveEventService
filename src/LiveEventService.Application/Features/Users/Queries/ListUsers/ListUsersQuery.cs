using AutoMapper;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Application.Common.Models;
using LiveEventService.Core.Users.User;
using MediatR;
using LiveEventService.Application.Features.Users.Queries.ListUsers;

namespace LiveEventService.Application.Features.Users.User.List;

public class ListUsersQuery : IRequest<BaseResponse<UserListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}

public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, BaseResponse<UserListDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ListUsersQueryHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserListDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        // Build specification
        var spec = new ListUsersSpecification(request.SearchTerm, request.IsActive);
        spec.ApplyPaging((request.PageNumber - 1) * request.PageSize, request.PageSize);

        // Get filtered and paged users
        var users = await _userRepository.ListAsync(spec, cancellationToken);
        // Get total count for pagination (without paging)
        var countSpec = new ListUsersSpecification(request.SearchTerm, request.IsActive);
        var totalCount = await _userRepository.CountAsync(countSpec, cancellationToken);

        // Map to DTOs
        var userDtos = _mapper.Map<List<UserDto>>(users);

        var result = new UserListDto
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return BaseResponse<UserListDto>.Succeeded(result);
    }
}
