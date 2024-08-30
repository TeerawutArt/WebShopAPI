using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebShoppingAPI.DTOs.Request.Discount;
using WebShoppingAPI.DTOs.Response.Discount;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[Route("[controller]")]
[ApiController]

public class DiscountsController(AppDbContext appDbContext, UserManager<UserModel> userManager, PriceCalculateService priceCalculate) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> _userManager = userManager;

    private readonly PriceCalculateService _priceCalculate = priceCalculate;


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDiscount(CreateDiscountDTO req)
    {

        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var newDiscount = new DiscountModel
            {
                Name = req.DiscountName,
                Description = req.DiscountDescription,
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                DiscountRate = req.DiscountRate,
                IsDiscounted = true,
                IsDiscountPercent = req.IsDiscountPercent,
            };
            _appDbContext.Discounts.Add(newDiscount);
            await _appDbContext.SaveChangesAsync();
            //ตรวจสอบlogic กรณีเจาะจงสินค้าที่จะลด
            if (req.ProductId.Any())
            {//เอา Discount ไปผูกในแต่ละ product  logic เพิ่มเติมคือ ถ้ามีการลดราคาอยู่แล้วจะไม่ไปแทนที่ ต้องยกเลิกตัวเก่าก่อน
                foreach (var productId in req.ProductId)
                {
                    var products = await _appDbContext.Products.Where(p => p.Id == productId && p.DiscountId == null).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = newDiscount.Id;
                        product.DiscountPrice = _priceCalculate.DiscountPrice(product.Price, req.DiscountRate, req.IsDiscountPercent);
                    }
                }
            }
            if (req.CategoriesId.Any())
            {
                foreach (var categoryId in req.CategoriesId)
                {

                    var products = await _appDbContext.Products.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId && p.DiscountId == null)).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = newDiscount.Id;
                        product.DiscountPrice = _priceCalculate.DiscountPrice(product.Price, req.DiscountRate, req.IsDiscountPercent);
                    }
                }
            }
            await _appDbContext.SaveChangesAsync();
            return NoContent();

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDiscounts()
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var discounts = await _appDbContext.Discounts.Select(d => new GetDiscountDTO
            {
                DiscountId = d.Id,
                DiscountName = d.Name,
                Description = d.Description,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                IsDiscountPercent = d.IsDiscountPercent,
                DiscountRate = d.DiscountRate,
                IsDiscounted = d.IsDiscounted,
                DiscountProductID = d.Products.Select(p => p.Id).ToList()
            }).ToListAsync();
            return Ok(discounts);

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDiscount(Guid id, UpdateDiscountDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var curDiscount = await _appDbContext.Discounts.FirstOrDefaultAsync(d => d.Id == id);
            if (curDiscount == null) return NotFound();
            //อัปเดทข้อมูลทั่วไป
            curDiscount.Name = req.DiscountName.IsNullOrEmpty() ? curDiscount.Name : req.DiscountName;
            curDiscount.Description = req.DiscountDescription.IsNullOrEmpty() ? curDiscount.Description : req.DiscountDescription;
            curDiscount.EndTime = req.EndTime;

            if (req.ProductId.Any())
            {//เงื่อนไขเดิม จะเพิ่มสินค้าได้ก็ต่อเมื่อมันยังไม่ลดราคา ถ้าจะลดต้องไปยกเลิกก่อน
                foreach (var productId in req.ProductId)
                {
                    var products = await _appDbContext.Products.Where(p => p.Id == productId && p.DiscountId == null).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = curDiscount.Id;
                        product.DiscountPrice = _priceCalculate.DiscountPrice(product.Price, curDiscount.DiscountRate, curDiscount.IsDiscountPercent);
                    }
                }
            }
            if (req.CategoriesId.Any())
            {
                foreach (var categoryId in req.CategoriesId)
                {

                    var products = await _appDbContext.Products.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId && p.DiscountId == null)).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = curDiscount.Id;
                        product.DiscountPrice = _priceCalculate.DiscountPrice(product.Price, curDiscount.DiscountRate, curDiscount.IsDiscountPercent);
                    }
                }
            }
            await _appDbContext.SaveChangesAsync();
            return NoContent();

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPut("CancelDiscount/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelDiscountProduct(Guid id, CancelDiscountDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            if (req.ProductId.Any())
            {
                foreach (var productId in req.ProductId)
                {
                    var products = await _appDbContext.Products.Where(p => p.Id == productId && p.DiscountId == id).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = null;
                        product.DiscountPrice = product.Price;
                    }
                }
            }
            if (req.CategoriesId.Any())
            {
                foreach (var categoryId in req.CategoriesId)
                {

                    var products = await _appDbContext.Products.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId && p.DiscountId == id)).ToListAsync();
                    foreach (var product in products)
                    {
                        product.DiscountId = null;
                        product.DiscountPrice = product.Price;
                    }
                }
            }
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
    public async Task<IActionResult> RemoveDiscount(Guid id)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var targetDiscount = await _appDbContext.Discounts.Include(d => d.Products).FirstOrDefaultAsync(d => d.Id == id);
            if (targetDiscount == null) return NotFound();
            // ลบความสัมพันธ์ระหว่าง Discount กับ Products
            foreach (var product in targetDiscount.Products)
            {
                product.DiscountId = null;  // ตั้ง DiscountId กลับไปเป็น null
                product.DiscountPrice = product.Price; //ตั้งลดราคากลับไปเป็นราคาเต็ม
            }
            await _appDbContext.SaveChangesAsync();
            _appDbContext.Discounts.Remove(targetDiscount);
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


