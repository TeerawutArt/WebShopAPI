using System.ComponentModel;
using System.Security.Claims;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Response.Category;

namespace WebShoppingAPI.Controllers;

[ApiController]
[Authorize]
[Route("[Controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class ProductsController(AppDbContext appDbContext, FileService fileService, UserManager<UserModel> userManager) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly FileService _fileService = fileService;
    private readonly UserManager<UserModel> userManager = userManager;
    private readonly string defaultImageURLForProduct = "no-image-available.jpg";
    //locale time zone
    private readonly TimeZoneInfo localeTimeZone = TimeZoneInfo.Local;




    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsDTO req)
    {
        try
        {
            var query = _appDbContext.Products.AsQueryable();
            var curDate = DateTime.UtcNow;
            var curUserId = User.FindFirstValue("uid");
            UserModel? user = await userManager.FindByIdAsync(curUserId!);

            if (req.OnlyMyItem)
            {
                query = query.Where(e => e.UserId == user!.Id);
            }
            if (!string.IsNullOrWhiteSpace(req.Keyword)) //IsNullOrWhiteSpace คือ ไม่เอา spacebar ด้วย
            {
                query = query.Where(e => e.Name.ToLower().Contains(req.Keyword.ToLower())  //Contains มีอักษรบางส่วน
                || e.Description!.ToLower().Contains(req.Keyword.ToLower()));
            }
            //ตรวจสอบวันสิ้นสุดกิจกรรม (role customer ไม่โชว์ หรือติ้กไม่โชว์)
            var userRole = await userManager.GetRolesAsync(user!);
            if (userRole.Any(role => role == "Customer") || req.ExpireProduct)
            {
                query = query.Where(e => e.UpdatedTime >= curDate);
            }
            var totalRecords = await query.CountAsync(); //นับค่าจาก query ทั้งหมด
            var pageIndex = req.PageIndex;
            var pageSize = req.PageSize;
            var skipRecords = (pageIndex - 1) * pageSize; //ใน db ค่าเริ่มที่ 0 แต่ที่ส่งมาจาก fontEnd จะส่งมาเป็น 1 เลยต้อง -1
            var products = await query.OrderBy(e => e.UpdatedTime).Skip(skipRecords).Take(pageSize).Select(row => new ProductDTO
            {
                ProductId = row.Id,
                ProductImageURL = row.ProductImageURL,
                ProductName = row.Name,
                ProductDescription = row.Description,
                ProductTotalAmount = row.TotalAmount,
                ProductCreatedBy = row.CreatedBy,
                ProductUpdatedBy = row.UpdatedBy,
                CreatedTime = row.CreatedTime,
                UpdatedTime = row.UpdatedTime,
                Price = row.Price,
                TotalScore = row.TotalScore,
                IsAvailable = row.IsAvailable,
                Categories = row.ProductCategories.Select(pc => new CategoriesDTO
                {
                    Id = pc.CategoryId,
                    Name = pc.Category!.Name,
                    Description = pc.Category.Description,
                    NormalizedName = pc.Category.NormalizedName,
                }).ToList()
            }).ToListAsync();
            var res = new PagingDTO<ProductDTO>
            {
                TotalRecords = totalRecords,
                Items = products
            };
            //แปลงเวลากลับจาก UTC กลับเป็น locale
            foreach (var item in res.Items)
            {
                item.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(item.CreatedTime, localeTimeZone);
                item.UpdatedTime = TimeZoneInfo.ConvertTimeFromUtc(item.UpdatedTime, localeTimeZone);
            }

            return Ok(res);

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }


    }
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostProduct(CreateProductDTO req)
    {
        try
        {

            var userId = User.FindFirstValue("uid");
            UserModel? user = await userManager.FindByIdAsync(userId!);
            string createdImageName = defaultImageURLForProduct;
            if (req.ProductImage is not null)
            {
                if (req.ProductImage!.Length > 1 * 1024 * 1024) return StatusCode(StatusCodes.Status400BadRequest, "ขนาดไฟล์รูปต้องไม่ใหญ่เกิน 1 MB");
                string[] allowFileExtensions = [".jpg", ".jpeg", ".png"];
                createdImageName = await _fileService.SaveFileAsync(req.ProductImage!, allowFileExtensions);
            }

            var newProduct = new ProductModel
            {
                ProductImageURL = createdImageName,
                Name = req.Name,
                Description = req.Description,
                TotalAmount = Int32.Parse(req.TotalAmount!),
                CreatedBy = User.FindFirstValue("name"),
                CreatedTime = DateTime.UtcNow,
                Price = double.Parse(req.Price!),
                UserId = user!.Id,
                IsAvailable = true,
            };
            //เพิ่มลง db แล้วเซฟ เพื่อสร้าง GUID ก่อน
            _appDbContext.Products.Add(newProduct);
            await _appDbContext.SaveChangesAsync();
            //สร้างความสัมพันธ์ many-to-many
            var newProductCategory = new ProductCategoryModel
            {
                ProductId = newProduct.Id,
                CategoryId = req.CategoryId
            };

            _appDbContext.ProductCategories.Add(newProductCategory);
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
    [Authorize(Roles = "Sale,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutProduct(Guid id, UpdateProductDTO req)
    {
        try
        {
            var curUserId = User.FindFirstValue("uid");
            var user = await userManager.FindByIdAsync(curUserId!);
            //ต้อง Include เพื่อเข้าถึงการใช้งาน ProductCategories ด้วย ถ้าไม่เข้าถึงจะทำงานไม่ถูกต้อง กรณีนี้คือใช้ RemoveRange ไม่ได้
            var curProduct = await _appDbContext.Products.Include(p => p.ProductCategories).FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);
            if (curProduct == null) return NotFound();
            if (req.ProductImage is not null)
            {
                if (req.ProductImage.Length > 1 * 1024 * 1024) return StatusCode(StatusCodes.Status400BadRequest, "ขนาดไฟล์รูปต้องไม่ใหญ่เกิน 1 MB");
                string[] allowFileExtensions = [".jpg", ".jpeg", ".png"];
                string createdImageName = await _fileService.SaveFileAsync(req.ProductImage, allowFileExtensions);
                if (curProduct.ProductImageURL != defaultImageURLForProduct) _fileService.DeleteFile(curProduct.ProductImageURL!);
                curProduct.ProductImageURL = createdImageName;
            }
            else { curProduct.ProductImageURL = curProduct.ProductImageURL; }
            curProduct.Name = req.Name!;
            curProduct.Description = req.Description;
            curProduct.TotalAmount = Int32.Parse(req.TotalAmount!);
            curProduct.UpdatedBy = User.FindFirstValue("name");
            curProduct.Price = double.Parse(req.Price!);
            curProduct.UpdatedTime = DateTime.UtcNow;
            _appDbContext.Products.Update(curProduct);
            //update ความสัมพันธ์ (ลบเก่าสร้างใหม่)
            _appDbContext.ProductCategories.RemoveRange(curProduct.ProductCategories.ToList());
            foreach (var categoryId in req.NewCategoryId)
            {
                var newProductCategory = new ProductCategoryModel
                {
                    ProductId = id,
                    CategoryId = categoryId
                };
                _appDbContext.ProductCategories.Add(newProductCategory);

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

    [HttpPut("ChangeAvailable/{id}")]
    [Authorize(Roles = "Sale,Admin")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductAvailable(Guid id, UpdateProductAvailableDTO req)
    {
        try
        {
            var curUserId = User.FindFirstValue("uid");
            var user = await userManager.FindByIdAsync(curUserId!);
            var curProduct = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);
            if (curProduct == null) return NotFound();

            curProduct.IsAvailable = req.IsAvailable;
            _appDbContext.Products.Update(curProduct);
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
    [Authorize(Roles = "Sale,Admin")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            ProductModel? curProduct;
            var curUserId = User.FindFirstValue("uid");
            var user = await userManager.FindByIdAsync(curUserId!);
            if (User.IsInRole("Admin")) curProduct = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
            else curProduct = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user!.Id);
            if (curProduct == null) return NotFound();
            _appDbContext.Products.Remove(curProduct);
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