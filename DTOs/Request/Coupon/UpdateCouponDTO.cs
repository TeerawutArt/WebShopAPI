using System;

namespace WebShoppingAPI.DTOs.Request.Coupon;

public class UpdateCouponDTO
{

    public string? Description { get; set; }
    public DateTime EndTime { get; set; }
    public int Amount { get; set; }
    public bool IsAvailable { get; set; }
    public double MaxDiscount { get; set; }
    public double MinimumPrice { get; set; } //ราคาขั้นต่ำที่ใช้คูปองได้

}
