using System;
using WebShoppingAPI.DTOs.Response.Category;

namespace WebShoppingAPI.DTOs.Response.Discount;

public class ProductDiscountListDTO
{
    public string? ProductImageURL { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public double Price { get; set; }
    public double DiscountPrice { get; set; }
    public List<CategoriesDTO> Categories { get; set; } = new List<CategoriesDTO>();
}
