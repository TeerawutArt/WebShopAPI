using System;

namespace WebShoppingAPI.DTOs.Response;

public class OrderProductDTO
{
    public Guid OrderProductId { get; set; }
    public DateTime SelectedTime { get; set; }
    public DateTime ExpiredPaidTime { get; set; }
    public bool IsPaid { get; set; }
    public string? TransportInfo { get; set; }
    public string? Status { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }

}
