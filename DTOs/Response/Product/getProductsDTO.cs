using System;
using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Response;

public class GetProductsDTO
{
    public bool OnlyMyItem { get; set; }
    public bool AvailableProduct { get; set; }
    public string? Keyword { get; set; }

    [Range(1, 1000)] //กำหนดขอบเขต ของ PageIndex
    public int PageIndex { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 5;
}
