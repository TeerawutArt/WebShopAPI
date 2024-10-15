using System;

namespace WebShoppingAPI.DTOs.Request.Coupon;

public class GetCouponByOrderDTO
{
    public Guid OrderId { get; set; }
}
