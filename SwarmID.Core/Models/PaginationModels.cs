namespace SwarmID.Core.Models;

/// <summary>
/// Represents pagination information for data requests
/// </summary>
public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>
/// Represents paginated response with data and pagination metadata
/// </summary>
/// <typeparam name="T">Type of data being paginated</typeparam>
public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>
/// Pagination metadata for responses
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
