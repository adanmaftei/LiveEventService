using AutoMapper;
using LiveEventService.Core.Events;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.EventRegistration;
using LiveEventService.Application.Features.Users.User;
using UserEntity = LiveEventService.Core.Users.User.User;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile defining mappings between Core domain entities and Application DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Event mappings
        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.StartDateTime, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDateTime, opt => opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location)) // Map Location to Address
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.Capacity - src.Registrations.Count(r => r.Status == LiveEventService.Core.Registrations.EventRegistration.RegistrationStatus.Confirmed)));

        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDateTime))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDateTime))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsPublished, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizerId, opt => opt.Ignore())
            .ForMember(dest => dest.Registrations, opt => opt.Ignore());

        CreateMap<UpdateEventDto, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.OrganizerId, opt => opt.Ignore())
            .ForMember(dest => dest.Registrations, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // User mappings
        CreateMap<UserEntity, UserDto>();

        CreateMap<CreateUserDto, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.EventRegistrations, opt => opt.Ignore());

        CreateMap<UpdateUserDto, UserEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdentityId, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.EventRegistrations, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Event Registration mappings
        CreateMap<EventRegistrationEntity, EventRegistrationDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}".Trim()))
            .ForMember(dest => dest.UserEmail,
                opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CreateEventRegistrationDto, EventRegistrationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.PositionInQueue, opt => opt.Ignore())
            .ForMember(dest => dest.Event, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
