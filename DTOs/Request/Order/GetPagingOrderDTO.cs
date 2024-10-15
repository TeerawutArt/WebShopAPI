using System;
using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request.Order;

public class GetPagingOrderDTO
{
    [Range(1, 1000)] //กำหนดขอบเขต ของ PageIndex
    public int PageIndex { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 5;
}
