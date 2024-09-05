using System;

namespace WebShoppingAPI.DTOs.Request.Discount;

public class UpdateDiscountDTO
{
    public string? DiscountName { get; set; }
    public string? DiscountDescription { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }

    public List<Guid> CategoriesId { get; set; } = new List<Guid>();
    public List<Guid> ProductId { get; set; } = new List<Guid>();

}
