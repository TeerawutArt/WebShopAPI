using System;

namespace WebShoppingAPI.DTOs.Request.Order;

public class ConfirmOrderDTO
{
    public Guid OrderId { get; set; }
    public required string AddressInfo { get; set; }
    public bool Transaction { get; set; }


}
