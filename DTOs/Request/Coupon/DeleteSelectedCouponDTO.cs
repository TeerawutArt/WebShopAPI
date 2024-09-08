using System;

namespace WebShoppingAPI.DTOs.Request.Coupon;

public class DeleteSelectedCouponDTO
{
    public List<Guid> SelectedCouponId { get; set; } = new List<Guid>();
}
