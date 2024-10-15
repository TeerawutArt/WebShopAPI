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
using WebShoppingAPI.DTOs.Request.Product;

namespace WebShoppingAPI.Controllers;

[ApiController]
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
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsDTO req)
    {
        try
        {
            var query = _appDbContext.Products.AsQueryable();
            var curDate = DateTime.UtcNow;
            UserModel? user = await userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user == null)
            {
                query = query.Where(e => e.IsAvailable == true);
            }
            if (user != null && req.ManageProductMode)
            {
                query = query.Where(e => e.UserId == user!.Id);
            }
            if (user != null)
            {
                var userRole = await userManager.GetRolesAsync(user!);
                //ตรวจสอบ Product ว่ามีอยู่ไหม (role customer ไม่โชว์ หรือติ้กไม่โชว์)
                if (userRole.Any(role => role == "Customer") || req.HideDisableProduct)
                {
                    query = query.Where(e => e.IsAvailable == true);
                }
            }
            if (!string.IsNullOrWhiteSpace(req.Keyword)) //IsNullOrWhiteSpace คือ ไม่เอา spacebar ด้วย
            {
                query = query.Where(e => e.Name.ToLower().Contains(req.Keyword.ToLower())  //Contains มีอักษรบางส่วน
                || e.Description!.ToLower().Contains(req.Keyword.ToLower()));
            }
            var totalRecords = await query.CountAsync(); //นับค่าจาก query ทั้งหมด
            var pageIndex = req.PageIndex;
            var pageSize = req.PageSize;
            var skipRecords = (pageIndex - 1) * pageSize; //ใน db ค่าเริ่มที่ 0 แต่ที่ส่งมาจาก fontEnd จะส่งมาเป็น 1 เลยต้อง -1
            var products = await query.OrderByDescending(e => e.CreatedTimeUTC).Skip(skipRecords).Take(pageSize).Select(row => new ProductListDTO
            {
                ProductId = row.Id,
                ProductImageURL = row.ProductImageURL,
                ProductName = row.Name,
                Description = row.Description,
                ProductTotalAmount = row.TotalAmount,
                ProductSoldAmount = row.SoldAmount,
                Price = row.Price,
                DiscountPrice = row.DiscountPrice,
                TotalScore = row.TotalScore,
                IsAvailable = row.IsAvailable,
                IsDiscounted = row.Discount != null ? row.Discount.IsDiscounted : false,
                DiscountStartDate = row.Discount != null
                ? TimeZoneInfo.ConvertTimeFromUtc(row.Discount.StartTimeUTC, localeTimeZone)
                : new DateTime(),
                DiscountEndDate = row.Discount != null
                ? TimeZoneInfo.ConvertTimeFromUtc(row.Discount.EndTimeUTC, localeTimeZone)
                : new DateTime(),
                IsDiscountPercent = row.Discount != null ? row.Discount.IsDiscountPercent : false,
                DiscountRate = row.Discount != null ? row.Discount.DiscountRate : 0,
                Categories = row.ProductCategories.Select(pc => new CategoriesDTO
                {
                    Id = pc.Category!.Id,
                    Name = pc.Category!.Name,
                    Code = pc.Category.NormalizedName,
                    Description = pc.Category.Description,
                }).ToList()
            }).ToListAsync();
            var res = new PagingDTO<ProductListDTO>
            {
                TotalRecords = totalRecords,
                Items = products
            };


            return Ok(res);

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }


    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductDetail(Guid id)
    {
        try
        {

            UserModel? user = await userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var userRole = await userManager.GetRolesAsync(user!);
            //ตรวจสอบ Product ว่ามีอยู่ไหม (role customer ไม่โชว์ หรือติ้กไม่โชว์)
            if (userRole.Any(role => role == "Customer"))
            {
                var products = await _appDbContext.Products.Where(p => p.Id == id).Include(p => p.Discount).Select(row => new ProductDTO
                {
                    ProductId = row.Id,
                    ProductImageURL = row.ProductImageURL,
                    ProductName = row.Name,
                    ProductDescription = row.Description,
                    ProductTotalAmount = row.TotalAmount,
                    ProductSoldAmount = row.SoldAmount,
                    Price = row.Price,
                    DiscountPrice = row.DiscountPrice,
                    TotalScore = row.TotalScore,
                    IsAvailable = row.IsAvailable,
                    IsDiscounted = row.Discount != null ? row.Discount.IsDiscounted : false,
                    DiscountStartDate = row.Discount != null ? row.Discount.StartTimeUTC : new DateTime(),
                    DiscountEndDate = row.Discount != null ? row.Discount.EndTimeUTC : new DateTime(),
                    IsDiscountPercent = row.Discount != null ? row.Discount.IsDiscountPercent : false,
                    DiscountRate = row.Discount != null ? row.Discount.DiscountRate : 0,
                    Categories = row.ProductCategories.Select(pc => new CategoriesDTO
                    {
                        Id = pc.Category!.Id,
                        Name = pc.Category!.Name,
                        Code = pc.Category.NormalizedName,
                    }).ToList()
                }).ToListAsync();
                foreach (var item in products)
                {
                    item.DiscountStartDate = TimeZoneInfo.ConvertTimeFromUtc(item.DiscountStartDate, localeTimeZone);
                    item.DiscountEndDate = TimeZoneInfo.ConvertTimeFromUtc(item.DiscountEndDate, localeTimeZone);
                }
                return Ok(products);
            }
            else
            {
                var products = await _appDbContext.Products.Where(p => p.Id == id).Include(p => p.Discount).Select(row => new ProductDTO
                {
                    ProductId = row.Id,
                    ProductImageURL = row.ProductImageURL,
                    ProductName = row.Name,
                    ProductDescription = row.Description,
                    ProductTotalAmount = row.TotalAmount,
                    ProductSoldAmount = row.SoldAmount,
                    ProductCreatedBy = row.CreatedBy,
                    ProductCreatedTime = row.CreatedTimeUTC,
                    Price = row.Price,
                    DiscountPrice = row.DiscountPrice,
                    TotalScore = row.TotalScore,
                    IsAvailable = row.IsAvailable,
                    IsDiscounted = row.Discount != null ? row.Discount.IsDiscounted : false,
                    DiscountStartDate = row.Discount != null ? row.Discount.StartTimeUTC : new DateTime(),
                    DiscountEndDate = row.Discount != null ? row.Discount.EndTimeUTC : new DateTime(),
                    IsDiscountPercent = row.Discount != null ? row.Discount.IsDiscountPercent : false,
                    DiscountRate = row.Discount != null ? row.Discount.DiscountRate : 0,
                    Categories = row.ProductCategories.Select(pc => new CategoriesDTO
                    {
                        Name = pc.Category!.Name,
                        Code = pc.Category.NormalizedName,
                    }).ToList()
                }).ToListAsync();
                //แปลงเวลากลับจาก UTC กลับเป็น locale
                foreach (var item in products)
                {
                    item.ProductCreatedTime = TimeZoneInfo.ConvertTimeFromUtc(item.ProductCreatedTime, localeTimeZone);
                    item.DiscountStartDate = TimeZoneInfo.ConvertTimeFromUtc(item.DiscountStartDate, localeTimeZone);
                    item.DiscountEndDate = TimeZoneInfo.ConvertTimeFromUtc(item.DiscountEndDate, localeTimeZone);
                }
                return Ok(products);
            }
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
                CreatedTimeUTC = DateTime.UtcNow,
                Price = double.Parse(req.Price!),
                DiscountPrice = double.Parse(req.Price!),
                UserId = user!.Id,
                IsAvailable = true,

            };
            //เพิ่มลง db แล้วเซฟ เพื่อสร้าง GUID ก่อน
            _appDbContext.Products.Add(newProduct);
            await _appDbContext.SaveChangesAsync();
            //สร้างความสัมพันธ์ many-to-many  ใช้ loop เพื่อกรณี 1 Product มีหลาย Category

            foreach (var categoryId in req.CategoryId)
            {
                var newProductCategory = new ProductCategoryModel
                {
                    ProductId = newProduct.Id,
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
            //ต้อง Include เพื่อเข้าถึงการใช้งาน ProductCategories ด้วย 
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
            curProduct.Name = req.Name == null ? curProduct.Name : req.Name;
            curProduct.Description = req.Description;
            curProduct.TotalAmount = Int32.Parse(req.TotalAmount!);
            curProduct.UpdatedBy = User.FindFirstValue("name");
            curProduct.Price = double.Parse(req.Price!);
            curProduct.UpdatedTimeUTC = DateTime.UtcNow;
            _appDbContext.Products.Update(curProduct);
            //update ความสัมพันธ์ (ลบเก่าสร้างใหม่)
            _appDbContext.ProductCategories.RemoveRange(curProduct.ProductCategories.ToList());
            foreach (var categoryId in req.CategoryId)
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

    [HttpDelete("Delete/Selected")]
    [Authorize(Roles = "Sale,Admin")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSelectedProduct(DeleteSelectedProductDTO req)
    {
        try
        {
            if (req.SelectedProductId == null || !req.SelectedProductId.Any())
            {
                return NotFound();
            }
            // ดึงสินค้าที่ต้องการลบทั้งหมด
            var selectedProducts = await _appDbContext.Products
                .Where(p => req.SelectedProductId.Contains(p.Id))
                .ToListAsync();

            if (!selectedProducts.Any())
            {
                return NotFound();
            }
            // ลบสินค้าที่ดึงมา
            _appDbContext.Products.RemoveRange(selectedProducts);
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