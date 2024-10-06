using System;

namespace WebShoppingAPI.DTOs.Response.Order;

public class OrderDTO
{
    public Guid OrderId { get; set; }
    public string? OrderUserName { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? Status { get; set; } //สำเร็จ,อยู่ระหว่างขนส่ง,ยกเลิก,ฯลฯ
    public string? TransportInfo { get; set; } //เก็บพวกรหัสค้นหามั้ง
    public double TotalPrice { get; set; }
    public double TransportPrice { get; set; }

    public DateTime TransactionTime { get; set; }
    public Guid UserId { get; set; }

    public List<OrderProductDTO> OrderProducts { get; set; } = new List<OrderProductDTO>();
}
