using System;
using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request;

public class DefaultPagingDTO
{
    [Range(1, 1000)] //กำหนดขอบเขต ของ PageIndex
    public int PageIndex { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
}
