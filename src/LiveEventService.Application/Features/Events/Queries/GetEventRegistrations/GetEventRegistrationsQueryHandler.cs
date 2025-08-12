using AutoMapper;
using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using LiveEventService.Core.Users.User;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using LiveEventService.Application.Features.Events.Queries.GetEventRegistrations;

namespace LiveEventService.Application.Features.Events.EventRegistration.Get;

public class GetEventRegistrationsQueryHandler : IQueryHandler<GetEventRegistrationsQuery, BaseResponse<EventRegistrationListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRepository<EventRegistrationEntity> _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetEventRegistrationsQueryHandler(
        IEventRepository eventRepository,
        IRepository<EventRegistrationEntity> registrationRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EventRegistrationListDto>> Handle(
        GetEventRegistrationsQuery request, 
        CancellationToken cancellationToken)
    {
        // Verify event exists (read-only check)
        var eventEntity = await _eventRepository.GetByIdReadOnlyAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return BaseResponse<EventRegistrationListDto>.Failed("Event not found");
        }

        // Build specification
        var spec = new GetEventRegistrationsSpecification(request.EventId, request.Status, request.UserId);
        spec.ApplyPaging((request.PageNumber - 1) * request.PageSize, request.PageSize);

        // Get filtered and paged registrations using read-only query
        var registrations = await _registrationRepository.ListReadOnlyAsync(spec, cancellationToken);
        // Get total count for pagination using optimized count specification (no includes)
        var countSpec = new GetEventRegistrationsCountSpecification(request.EventId, request.Status, request.UserId);
        var totalCount = await _registrationRepository.CountAsync(countSpec, cancellationToken);

        // Map to DTOs (AutoMapper will handle the includes from the specification)
        var registrationDtos = registrations.Select(er => _mapper.Map<EventRegistrationDto>(er)).ToList();

        var result = new EventRegistrationListDto
        {
            Items = registrationDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return BaseResponse<EventRegistrationListDto>.Succeeded(result);
    }
}
