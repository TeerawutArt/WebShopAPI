using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class CouponModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Amount { get; set; }
    public double Discount { get; set; }
    public bool IsDiscountPercent { get; set; }
    public double MaxDiscount { get; set; }

    [ForeignKey("OrderModel")]
    public Guid OrderId { get; set; }
    public OrderModel? Orders { get; set; }
}
