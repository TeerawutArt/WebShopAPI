using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class OrderModel
{
    public Guid Id { get; set; }
    public DateTime OrderTimeUTC { get; set; }
    public DateTime ExpiryTimeUTC { get; set; }
    public bool IsPaid { get; set; }
    public string? Status { get; set; } //สำเร็จ,อยู่ระหว่างขนส่ง,ยกเลิก,ฯลฯ
    public string? TransportInfo { get; set; } //เก็บพวกรหัสค้นหามั้ง

    public double TransportPrice { get; set; }
    public double TotalPrice { get; set; }

    public DateTime TransactionTimeUTC { get; set; }
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? Users { get; set; }
    public ICollection<OrderProductModel> OrderProducts { get; set; } = new List<OrderProductModel>();
}
