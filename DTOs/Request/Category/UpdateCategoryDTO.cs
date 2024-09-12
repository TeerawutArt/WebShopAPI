using System;

namespace WebShoppingAPI.DTOs.Request.Category;

public class UpdateCategoryDTO
{
    public string? Name { get; set; }
    public string? CodeName { get; set; }
    public string? Description { get; set; }

}
