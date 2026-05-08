namespace BenLanSystem.Models.DTOs;

public class TripSearchDto
{
    public int? OriginId { get; set; }
    public int? DestinationId { get; set; }
    public DateTime? DepartureDate { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? StatusName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TripSearchResultDto
{
    public long Id { get; set; }
    public int RouteId { get; set; }
    public string OriginName { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public DateTime DepartureTimeUtc { get; set; }
    public DateTime? ArrivalTimeUtc { get; set; }
    public decimal BasePrice { get; set; }
    public int AvailableSeats { get; set; }
    public string StatusName { get; set; } = "Open";
    public string? VehicleInfo { get; set; }
    public int? EstimatedMinutes { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}