

using WebShoppingAPI.DTOs.Response.Category;

namespace WebShoppingAPI.DTOs.Response;

public class ProductListDTO
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public int ProductTotalAmount { get; set; }
    public int ProductSoldAmount { get; set; }
    public double Price { get; set; }
    public double DiscountPrice { get; set; }
    public double TotalScore { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsDiscounted { get; set; }
    public DateTime DiscountStartDate { get; set; }
    public DateTime DiscountEndDate { get; set; }
    public bool IsDiscountPercent { get; set; }
    public double DiscountRate { get; set; }

    public string? ProductImageURL { get; set; }
    public List<CategoriesDTO> Categories { get; set; } = new List<CategoriesDTO>();
}
