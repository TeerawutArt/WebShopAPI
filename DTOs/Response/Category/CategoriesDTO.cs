using System;

namespace WebShoppingAPI.DTOs.Response.Category;

public class CategoriesDTO
{

    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid Id { get; set; }

}
