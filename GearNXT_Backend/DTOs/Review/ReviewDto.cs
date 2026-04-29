namespace GearNXT_Backend.DTOs.Review;

public class ReviewDto
{
    public int? CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
