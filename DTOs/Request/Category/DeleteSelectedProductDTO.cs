using System;

namespace WebShoppingAPI.DTOs.Request.Product;

public class DeleteSelectedCategoryDTO
{
    public List<Guid> CategoryId { get; set; } = new List<Guid>();
}
