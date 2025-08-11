namespace LiveEventService.Application.Features.Events.EventRegistration;

public class EventRegistrationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PositionInQueue { get; set; }
    public string? Notes { get; set; }
    public bool IsWaitlisted => PositionInQueue.HasValue && PositionInQueue > 0;
}

public class CreateEventRegistrationDto
{
    public Guid EventId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateEventRegistrationStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class EventRegistrationListDto
{
    public IEnumerable<EventRegistrationDto> Items { get; set; } = new List<EventRegistrationDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
