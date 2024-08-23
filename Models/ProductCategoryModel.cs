using System;

namespace WebShoppingAPI.Models;

//Many to Many
public class ProductCategoryModel
{

    public Guid ProductId { get; set; }
    public ProductModel? Product { get; set; }

    public Guid CategoryId { get; set; }
    public CategoryModel? Category { get; set; }

}
