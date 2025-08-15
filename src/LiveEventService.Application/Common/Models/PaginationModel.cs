namespace LiveEventService.Application.Common.Models;

/// <summary>
/// Simple pagination information used by list queries and responses.
/// </summary>
public class PaginationModel
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}
