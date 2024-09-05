using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class CouponModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime StartTimeUTC { get; set; }
    public DateTime EndTimeUTC { get; set; }
    public int Amount { get; set; }
    public double Discount { get; set; }
    public bool IsDiscountPercent { get; set; }
    public bool IsAvailable { get; set; }
    public double MaxDiscount { get; set; }
    public double MinimumPrice { get; set; } //ราคาขั้นต่ำที่ใช้คูปองได้

    public ICollection<UsedCouponModel> UsedCoupons { get; set; } = new List<UsedCouponModel>(); //ไว้ track การใช้คูปอง
}
