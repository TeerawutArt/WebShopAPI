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
    public double DiscountRate { get; set; }
    public bool IsDiscountPercent { get; set; }
    public bool IsDiscounted { get; set; }

    //one-to-many
    public ICollection<ProductModel> Products { get; set; } = new List<ProductModel>();
}
