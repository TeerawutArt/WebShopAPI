using System;

namespace WebShoppingAPI.DTOs.Request.Category;

public class CreateCategoryDTO
{

    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }

}
