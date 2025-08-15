using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Users.Queries.ExportUserData;

public class ExportUserDataQuery : IRequest<BaseResponse<ExportUserDataResult>>
{
    public string UserId { get; set; } = string.Empty;
}

public class ExportUserDataResult
{
    public string Json { get; set; } = string.Empty;
}
