using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace WebShoppingAPI.DTOs.Request;

public class CreateProductDTO
{
    public IFormFile? ProductImage { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อสินค้า")]
    public required string Name { get; set; }
    public string? Description { get; set; }
    [Required(ErrorMessage = "กรุณากรอกจำนวนสินค้า")]
    public string? TotalAmount { get; set; }
    public string? Price { get; set; }
    public Guid CategoryId { get; set; }

}
