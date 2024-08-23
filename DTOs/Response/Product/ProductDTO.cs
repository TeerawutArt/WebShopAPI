

using WebShoppingAPI.DTOs.Response.Category;

namespace WebShoppingAPI.DTOs.Response;

public class ProductDTO
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }

    public string? ProductDescription { get; set; }
    public int ProductTotalAmount { get; set; }
    public string? ProductCreatedBy { get; set; }
    public string? ProductUpdatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
    public double Price { get; set; }
    public double TotalScore { get; set; }
    public double NetPrice { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime ProductCreatedTime { get; set; }
    public DateTime ProductUpdatedTime { get; set; }
    public string? ProductImageURL { get; set; }
    public List<CategoriesDTO> Categories { get; set; } = new List<CategoriesDTO>();
}
