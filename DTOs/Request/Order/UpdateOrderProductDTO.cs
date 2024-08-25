using System;

namespace WebShoppingAPI.DTOs.Request;

public class UpdateOrderProductDTO
{

    public string? OrderStatus { get; set; } //สำเร็จ,อยู่ระหว่างขนส่ง,ยกเลิก,ฯลฯ
    public string? OrderTransportInfo { get; set; } //เก็บพวกรหัสค้นหามั้ง




}
