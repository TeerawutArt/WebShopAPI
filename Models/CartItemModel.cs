using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class CartItemModel
{
    public Guid Id { get; set; }

    //Cart
    [ForeignKey("CartModel")]
    public Guid CartId { get; set; }
    public CartModel? Cart { get; set; }
    //Product
    [ForeignKey("ProductModel")]
    public Guid ProductId { get; set; }
    public ProductModel? Product { get; set; }
    public int Quantity { get; set; }
}
