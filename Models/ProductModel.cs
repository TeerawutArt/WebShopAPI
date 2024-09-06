using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace WebShoppingAPI.Models;

public class ProductModel
{
    public Guid Id { get; set; }
    public string? ProductImageURL { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SoldAmount { get; set; }
    public int TotalAmount { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedTimeUTC { get; set; }
    public DateTime UpdatedTimeUTC { get; set; }
    public double Price { get; set; }

    public double DiscountPrice { get; set; }
    public double TotalScore { get; set; }
    public bool IsAvailable { get; set; }

    //many-to-one
    [ForeignKey("DiscountModel")]
    public Guid? DiscountId { get; set; }
    public DiscountModel? Discount { get; set; }

    //one-to-many
    public ICollection<OrderProductModel>? OrderProduct { get; set; }
    public ICollection<ReviewCommentModel>? ReviewComments { get; set; }
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? Users { get; set; }
    //many-to-many
    public ICollection<ProductCategoryModel> ProductCategories { get; set; } = new List<ProductCategoryModel>();
}
