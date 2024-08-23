/*
using System.Security.Claims;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WebShoppingAPI.Controllers;



[ApiController]
[Authorize]
[Route("[Controller]")]
[Consumes("application/json")]
[Produces("application/json")]

public class OrderProductsController(AppDbContext appDbContext, UserManager<UserModel> userManager) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> userManager = _userManager;


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrderProduct(CreateOrderProductDTO req, Boolean IsPaid)
    {
        try
        {
            var curProduct = await _appDbContext.Products.FirstOrDefaultAsync(e => e.Id == req.ProductId);
            var customerId = User.FindFirstValue("uid");
            var userName = User.FindFirstValue("name");
            UserModel? user = await _userManager.FindByIdAsync(customerId!);
            if (curProduct == null) return NotFound();
            if (curProduct.OrderAmount >= curProduct.TotalAmount) return Forbid(); //สินค้าหมด ใส่ Forbid ไปก่อน
            var status = "";
            if (IsPaid)
            {
                status = "อยู่ระหว่างขนส่ง";
            }
            else { status = "รอชำระเงิน"; }
            var newOrderProduct = new OrderProductModel
            {
                ProductId = req.ProductId,
                UserId = user!.Id,
                SelectedTime = DateTime.UtcNow,
                ExpiredPaidTime = DateTime.UtcNow.AddDays(req.ExpiryInDay),
                IsPaid = IsPaid,
                Status = status,
            };
            _appDbContext.Add(newOrderProduct);
            var result = await _appDbContext.SaveChangesAsync();
            if (result > 0)
            {
                //Update Amount ใน Product
                //SelectMany จะรวมฟิลทั้งหมดให้เป็น Collection
                var totalOrderProduct = _appDbContext.Products.Where(e => e.Id == req.ProductId).SelectMany(e => e.OrderProduct!).Count();
                curProduct.OrderAmount = totalOrderProduct;
                await _appDbContext.SaveChangesAsync();

            }
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
    public async Task<IActionResult> GetOrderProducts()
    {
        try
        {
            //ลบก่อนส่งไป
            var curDate = DateTime.UtcNow;
            var expiredProducts = await _appDbContext.OrderProducts.Where(s => s.ExpiredPaidTime >= curDate).ToListAsync();
            if (expiredProducts.Count != 0) _appDbContext.OrderProducts.RemoveRange(expiredProducts);
            await _appDbContext.SaveChangesAsync();
            var orderProducts = await _appDbContext.OrderProducts.Select(orderProduct => new OrderProductDTO
            {
                OrderProductId = orderProduct.Id,
                ProductId = orderProduct.ProductId,
                UserId = orderProduct.UserId,
                SelectedTime = orderProduct.SelectedTime,
                ExpiredPaidTime = orderProduct.ExpiredPaidTime,
                IsPaid = orderProduct.IsPaid,
                Status = orderProduct.Status,
                TransportInfo = orderProduct.TransportInfo,
            }).ToListAsync();
            return Ok(orderProducts);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrderProductById(Guid id)
    {
        try
        {
            var orderProduct = await _appDbContext.OrderProducts.FirstOrDefaultAsync(t => t.Id == id);
            if (orderProduct == null) return NotFound();
            var curOrderProduct = new OrderProductDTO
            {
                OrderProductId = orderProduct.Id,
                ProductId = orderProduct.ProductId,
                UserId = orderProduct.UserId,
                SelectedTime = orderProduct.SelectedTime,
                ExpiredPaidTime = orderProduct.ExpiredPaidTime,
                IsPaid = orderProduct.IsPaid,
                Status = orderProduct.Status,
                TransportInfo = orderProduct.TransportInfo,
            };
            return Ok(curOrderProduct);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Sale")]
    public async Task<IActionResult> UpdateOrderProduct(Guid id, UpdateOrderProductDTO req, Boolean IsPaid)
    {
        try
        {
            var curOrderProduct = await _appDbContext.OrderProducts.FirstOrDefaultAsync(t => t.Id == id);
            if (curOrderProduct == null) return NotFound();
             curOrderProduct.IsPaid = IsPaid; 
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
[Authorize(Roles = "Sale")]
public async Task<IActionResult> RemoveOrderProduct(Guid id)
{
    try
    {

        var curProduct = await _appDbContext.Products.FirstOrDefaultAsync(e => e.OrderProduct!.Any(t => t.Id == id));
        var curOrderProduct = await _appDbContext.OrderProducts.FirstOrDefaultAsync(t => t.Id == id);
        if (curOrderProduct == null) return NotFound();
        _appDbContext.OrderProducts.Remove(curOrderProduct);
        await _appDbContext.SaveChangesAsync();
        var totalOrderProduct = _appDbContext.Products.SelectMany(e => e.OrderProduct!).Count(t => t.ProductId == curProduct!.Id);
        curProduct!.SoldAmount = totalOrderProduct;
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
*/