using System;

namespace WebShoppingAPI.DTOs.Response.Order;

public class OrderSmallDetailDTO
{
    public Guid OrderId { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsPaid { get; set; }
    public bool UsedCoupon { get; set; }
    public string? Status { get; set; } //สำเร็จ,อยู่ระหว่างขนส่ง,ยกเลิก,ฯลฯ
    public double TotalPrice { get; set; }
    public double NetPrice { get; set; }
    public double TransportPrice { get; set; }

    public List<OrderProductDTO> OrderProducts { get; set; } = new List<OrderProductDTO>();

}
