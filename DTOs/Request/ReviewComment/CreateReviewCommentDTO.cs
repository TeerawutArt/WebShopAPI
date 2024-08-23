using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request;

public class CreateReviewCommentDTO
{

    public Guid ProductId { get; set; }
    [MaxLength(200, ErrorMessage = "จำนวนตัวอักษรต้องไม่เกิน 200ตัว")]
    public string? Title { get; set; }
    [MaxLength(200, ErrorMessage = "จำนวนตัวอักษรต้องไม่เกิน 200ตัว")]
    public string? Content { get; set; }
    public double Score { get; set; }


}
