using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request;

public class UpdateReviewCommentDTO
{

    [MaxLength(200, ErrorMessage = "จำนวนตัวอักษรต้องไม่เกิน 200ตัว")]
    public string? Title { get; set; }
    public double Score { get; set; }
    public string? Content { get; set; }



}
