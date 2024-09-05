using System;

namespace WebShoppingAPI.DTOs.Request.Product;

public class DeleteSelectedProductDTO
{
    public List<Guid> SelectedProductId { get; set; } = new List<Guid>();
}
