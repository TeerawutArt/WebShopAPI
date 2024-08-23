using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class CategoryModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }

    //many-to-many
    public ICollection<ProductCategoryModel> ProductCategories { get; set; } = new List<ProductCategoryModel>();

}
