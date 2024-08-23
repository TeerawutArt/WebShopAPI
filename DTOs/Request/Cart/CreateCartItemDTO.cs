using System;
using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request.Cart;

public class CreateCartItemDTO
{

    public Guid ProductId { get; set; }
    [Range(0, 1000)]
    public int Quantity { get; set; }

}
