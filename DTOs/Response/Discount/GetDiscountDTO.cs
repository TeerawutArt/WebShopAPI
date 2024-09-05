using System;

namespace WebShoppingAPI.DTOs.Response.Discount;

public class GetDiscountDTO
{

    public Guid DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }
    public bool IsDiscounted { get; set; }
    public List<ProductDiscountListDTO> DiscountProduct { get; set; } = new List<ProductDiscountListDTO>();


}
