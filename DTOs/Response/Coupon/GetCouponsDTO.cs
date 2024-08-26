using System;

namespace WebShoppingAPI.DTOs.Response.Coupon;

public class GetCouponsDTO
{
    public Guid CouponId { get; set; }
    public string? CouponName { get; set; }
    public string? CouponCode { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Amount { get; set; }
    public int UsedAmount { get; set; }
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }
    public bool IsCouponAvailable { get; set; }
    public double MaxDiscount { get; set; }
    public double MinimumPrice { get; set; }
}
