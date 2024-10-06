using System;

namespace WebShoppingAPI.DTOs.Response;

public class OrderProductDTO
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductImageURL { get; set; }
    public string? ProductName { get; set; }
    public int ProductQuantity { get; set; }
    public double ProductOriginalPrice { get; set; }
    public double UnitPrice { get; set; }
    public double NetPrice { get; set; }

}
