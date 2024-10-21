using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Request.Category;
using WebShoppingAPI.DTOs.Request.Product;
using WebShoppingAPI.DTOs.Response;
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
    public async Task<IActionResult> GetCategory([FromQuery] DefaultPagingDTO req)
    {
        try
        {
            var query = _appDbContext.Categories.AsQueryable();
            var pageIndex = req.PageIndex;
            var pageSize = req.PageSize;
            var skipRecords = (pageIndex - 1) * pageSize;
            var totalRecords = await _appDbContext.Categories.CountAsync();
            if (!string.IsNullOrWhiteSpace(req.Keyword)) //IsNullOrWhiteSpace คือ ไม่เอา spacebar ด้วย
            {
                query = query.Where(c => c.Name!.ToLower().Contains(req.Keyword.ToLower())  //Contains มีอักษรบางส่วน
                || c.NormalizedName!.ToLower().Contains(req.Keyword.ToLower())
                || c.Description!.ToLower().Contains(req.Keyword.ToLower()));
            }
            var categories = await query.Skip(skipRecords).Take(pageSize).Select(c => new CategoriesDTO
            {
                Name = c.Name,
                Code = c.NormalizedName,
                Description = c.Description,
                Id = c.Id
            }).ToListAsync();
            var res = new PagingDTO<CategoriesDTO>
            {
                TotalRecords = totalRecords,
                Items = categories
            };
            return Ok(res);
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
                NormalizedName = req.CodeName!.ToUpper(),
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
            curCategory.NormalizedName = req.CodeName;
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


    [HttpDelete("Selected")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategories(DeleteSelectedCategoryDTO req)
    {
        try
        {
            // ดึง categories ทั้งหมดที่ตรงกับ [id] ที่ส่งมา 
            var categoriesToDelete = await _appDbContext.Categories
                .Where(c => req.CategoryId.Contains(c.Id))
                .ToListAsync();

            if (categoriesToDelete == null || !categoriesToDelete.Any())
                return NotFound();

            _appDbContext.Categories.RemoveRange(categoriesToDelete);
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

