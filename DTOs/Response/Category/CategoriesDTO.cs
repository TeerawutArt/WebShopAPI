using System;

namespace WebShoppingAPI.DTOs.Response.Category;

public class CategoriesDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }

}
