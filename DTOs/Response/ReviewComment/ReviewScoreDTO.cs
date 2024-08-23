namespace WebShoppingAPI.DTOs.Response;

public class ReviewScoreDTO
{
    public Guid ReviewScoreId { get; set; }
    public Guid ReviewCommentId { get; set; }

    public Guid? UserId { get; set; }


    public bool Like { get; set; }

}
