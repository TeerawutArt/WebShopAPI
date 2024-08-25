using System;

namespace WebShoppingAPI.DTOs.Request;

public class CreateOrderProductDTO
{
    public Guid ProductId { get; set; }
    public int ProductQuantity { get; set; }


}
