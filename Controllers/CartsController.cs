using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Request.Cart;
using WebShoppingAPI.DTOs.Response.Cart;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CartsController(AppDbContext appDbContext, UserManager<UserModel> userManager) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> _userManager = userManager;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateCartItem(CreateCartItemDTO req)
    {
        try
        {
            //ตาม logic ตะกร้าควรจะถูกสร้างเมื่อลูกค้าทำการกดเลือกของ (ในที่นี้ไม่ได้สร้างพร้อม userถูกสร้าง)
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            //ตรวจก่อนว่า product ที่ส่งมามีอยู่หรือไม่
            var product = await _appDbContext.Products.FirstOrDefaultAsync(p => p.Id == req.ProductId);
            if (product is null) return NotFound();
            //ตรวจก่อนว่าผู้ใช้งานมี cart หรือยัง ถ้าไม่มีให้สร้าง
            var cart = await _appDbContext.Carts.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            if (cart is null)
            {
                var newCart = new CartModel
                {
                    UserId = user!.Id,
                };
                _appDbContext.Carts.Add(newCart);
                await _appDbContext.SaveChangesAsync();
                //ดึงข้อมูล cart ที่เพิ่งสร้างใหม่ออกมาใหม่
                cart = newCart;
            }
            //เช็ค logic ว่ามี product นั้นอยู่ในตระกร้าหรือยัง 
            var cartItem = await _appDbContext.CartItems.FirstOrDefaultAsync(c => c.CartId == cart!.Id && c.ProductId == req.ProductId);

            if (cartItem is null)
            {
                //ถ้าไม่มีให้สร้าง cartItem ลงไปใน cart 
                var newCartItem = new CartItemModel
                {
                    CartId = cart!.Id,
                    ProductId = req.ProductId,
                    Quantity = req.Quantity
                };
                _appDbContext.CartItems.Add(newCartItem);
            }
            else
            {
                //ถ้ามีอยู่แล้วให้ update จำนวนของ
                cartItem.Quantity += req.Quantity;
                _appDbContext.CartItems.Update(cartItem);
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
    [Authorize]
    public async Task<IActionResult> GetUserCart()
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null) return NotFound();
            var cart = await _appDbContext.Carts.Include(c => c.CartItems!).ThenInclude(c => c.Product).FirstOrDefaultAsync(c => c.UserId == user!.Id);
            //ThenInclude คือ Include เข้าไปใน Include อีกที
            if (cart is null) return NotFound();

            var cartItem = cart.CartItems!.Select(p => new CartItemDTO
            {
                ProductId = p.ProductId,
                ProductImageURL = p.Product!.ProductImageURL,
                ProductName = p.Product!.Name,
                Description = p.Product.Description,
                ProductPrice = p.Product!.Price,
                ProductTotalAmount = p.Product.TotalAmount,
                DiscountId = p.Product.DiscountId, //ใช้แทน IsDiscount ได้อยู่ (ไว้เป็นเงื่อนไขแสดงผลลัพธ์หน้าบ้าน)
                ProductDiscountPrice = p.Product!.DiscountPrice,
                Quantity = p.Product.TotalAmount > p.Quantity ? p.Quantity : p.Product.TotalAmount
            }).ToList();
            return Ok(cartItem);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPut("CartItem/product/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateQuantityCartItem(Guid id, UpdateQuantityItemDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            var curCart = await _appDbContext.Carts.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            var curCartItem = await _appDbContext.CartItems.FirstOrDefaultAsync(c => c.CartId == curCart!.Id && c.ProductId == id);
            if (curCartItem is null) return NotFound();

            curCartItem.Quantity = req.Quantity;
            _appDbContext.CartItems.Update(curCartItem);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpDelete("CartItem/product/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCartItem(Guid id)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            var curCart = await _appDbContext.Carts.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            var curCartItem = await _appDbContext.CartItems.FirstOrDefaultAsync(c => c.CartId == curCart!.Id && c.ProductId == id);
            if (curCartItem is null) return NotFound();

            _appDbContext.CartItems.Remove(curCartItem);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpDelete("CartItem/Clear")]
    [Authorize]
    public async Task<IActionResult> ClearAllCartItem()
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            var curCart = await _appDbContext.Carts.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            var allCartItem = await _appDbContext.CartItems.Where(c => c.CartId == curCart!.Id).ToListAsync();
            if (allCartItem is null) return NotFound();

            _appDbContext.CartItems.RemoveRange(allCartItem);
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

