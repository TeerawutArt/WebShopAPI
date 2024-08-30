using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace WebShoppingAPI.DTOs.Request;

public class UpdateProductDTO
{
    public IFormFile? ProductImage { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TotalAmount { get; set; }
    public string? Price { get; set; }

    public List<Guid> CategoryId { get; set; } = new List<Guid>();

}
