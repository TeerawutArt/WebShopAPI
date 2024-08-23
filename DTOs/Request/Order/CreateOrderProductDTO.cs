using System;

namespace WebShoppingAPI.DTOs.Request;

public class CreateOrderProductDTO
{

    public Double ExpiryInDay { get; set; }
    public Guid ProductId { get; set; }

}
