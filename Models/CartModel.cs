using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class CartModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? Users { get; set; }

    public Guid? CouponId { get; set; }
    [ForeignKey("CouponId")]
    public CouponModel? Coupon { get; set; }

    // one-to-many
    public ICollection<CartItemModel>? CartItems { get; set; }
}
