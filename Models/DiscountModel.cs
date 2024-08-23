using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class DiscountModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double Discount { get; set; }
    public double MaxDiscount { get; set; }
    public bool IsDiscountPercent { get; set; }

    [ForeignKey("ProductModel")]
    public Guid ProductId { get; set; }
    public ProductModel? Product { get; set; }
}
