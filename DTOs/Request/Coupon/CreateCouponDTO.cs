using System;

namespace WebShoppingAPI.DTOs.Request.Coupon;

public class CreateCouponDTO
{
    public string? CouponName { get; set; }
    public string? CouponCode { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Amount { get; set; }
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }

    public double MaxDiscount { get; set; }
    public double MinimumPrice { get; set; } //ราคาขั้นต่ำที่ใช้คูปองได้
}
