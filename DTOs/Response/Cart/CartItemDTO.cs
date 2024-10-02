using System;

namespace WebShoppingAPI.DTOs.Response.Cart;

public class CartItemDTO
{


    public Guid ProductId { get; set; }
    public string? ProductImageURL { get; set; }
    public required string ProductName { get; set; }
    public string? Description { get; set; }
    public Guid? DiscountId { get; set; }
    public double ProductPrice { get; set; }
    public int ProductTotalAmount { get; set; }
    public double ProductDiscountPrice { get; set; }
    public int Quantity { get; set; }

}
