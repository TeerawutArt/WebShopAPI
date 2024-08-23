namespace WebShoppingAPI.DTOs.Response;

public class ReviewCommentDTO
{
    public Guid ReviewCommentId { get; set; }
    public Guid ProductId { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? CreatedBy { get; set; }
    public double Score { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdateTime { get; set; }

}
