using System;

namespace WebShoppingAPI.DTOs.Request.Discount;

public class CancelDiscountDTO
{
    public List<Guid> CategoriesId { get; set; } = new List<Guid>();
    public List<Guid> ProductId { get; set; } = new List<Guid>();

}
