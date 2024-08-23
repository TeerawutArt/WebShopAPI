using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Request.Category;
using WebShoppingAPI.DTOs.Response.Category;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class CategoryController(AppDbContext appDbContext) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;


    [HttpGet]
    public async Task<IActionResult> GetCategory()
    {
        try
        {
            var categories = await _appDbContext.Categories.Select(c => new CategoriesDTO
            {
                Id = c.Id,
                Name = c.Name,
                NormalizedName = c.NormalizedName,
                Description = c.Description

            }).ToListAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(CreateCategoryDTO req)
    {
        try
        {
            var newCategory = new CategoryModel
            {
                Name = req.Name,
                NormalizedName = req.NormalizedName!.ToUpper(),
                Description = req.Description,
            };
            _appDbContext.Categories.Add(newCategory);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryDTO req)
    {
        try
        {
            var curCategory = await _appDbContext.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (curCategory == null) return NotFound();
            curCategory.Name = req.Name;
            curCategory.NormalizedName = req.NormalizedName;
            curCategory.Description = req.Description;
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var curCategory = await _appDbContext.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (curCategory == null) return NotFound();
            _appDbContext.Categories.Remove(curCategory);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
}

