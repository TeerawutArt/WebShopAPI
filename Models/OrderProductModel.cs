using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class OrderProductModel
{
    public Guid Id { get; set; }
    [ForeignKey("OrderModel")]
    public int OrderId { get; set; }
    public OrderModel? Orders { get; set; }
    //one-to-one
    [ForeignKey("ProductModel")]
    public Guid ProductId { get; set; }
    public ProductModel? Product { get; set; }
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double NetPrice { get; set; }
}
