using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class UsedCouponModel
{
    public Guid Id { get; set; }
    //track Coupon
    public Guid CouponId { get; set; }
    [ForeignKey("CouponId")]
    public CouponModel? Coupon { get; set; }
    //track user
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? User { get; set; }
    //track order
    public Guid OrderId { get; set; }
    [ForeignKey("OrderId")]
    public OrderModel? Order { get; set; }
}
