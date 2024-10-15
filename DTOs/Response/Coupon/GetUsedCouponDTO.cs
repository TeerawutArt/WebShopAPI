using System;

namespace WebShoppingAPI.DTOs.Response.Coupon;

public class GetUsedCouponDTO
{
    public Guid CouponId { get; set; }
    public string? CouponName { get; set; }
    public string? CouponCode { get; set; }
    public string? Description { get; set; }
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }
    public double MaxDiscount { get; set; }
    public double MinimumPrice { get; set; }
}
