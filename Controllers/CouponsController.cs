using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Request.Coupon;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.DTOs.Response.Coupon;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class CouponsController(AppDbContext appDbContext, UserManager<UserModel> userManager, PriceCalculateService priceCalculate) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly PriceCalculateService _priceCalculate = priceCalculate;
    private readonly TimeZoneInfo localeTimeZone = TimeZoneInfo.Local;

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCoupon(CreateCouponDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var checkCouponCode = await _appDbContext.Coupons.Where(c => c.Code!.ToLower() == req.CouponCode!.ToLower()).AnyAsync();
            if (checkCouponCode)
            {
                var errors = new[] { "โค้ดคูปองนี้ถูกสร้างไปแล้ว" };
                return BadRequest(new { Errors = errors });
            }
            var newCoupon = new CouponModel
            {
                Name = req.CouponName,
                Description = req.Description,
                Amount = req.Amount,
                Code = req.CouponCode,
                StartTimeUTC = req.StartTime,
                EndTimeUTC = req.EndTime,
                Discount = req.DiscountRate,
                IsDiscountPercent = req.IsDiscountPercent,
                IsAvailable = true,
                MaxDiscount = req.MaxDiscount,
                MinimumPrice = req.MinimumPrice,
            };
            _appDbContext.Coupons.Add(newCoupon);
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
    public async Task<IActionResult> GetCoupons([FromQuery] DefaultPagingDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }

            var query = _appDbContext.Coupons.AsQueryable();
            var pageIndex = req.PageIndex;
            var pageSize = req.PageSize;
            var skipRecords = (pageIndex - 1) * pageSize;
            var totalRecords = await _appDbContext.Coupons.CountAsync();
            if (!string.IsNullOrWhiteSpace(req.Keyword)) //IsNullOrWhiteSpace คือ ไม่เอา spacebar ด้วย
            {
                query = query.Where(e => e.Name!.ToLower().Contains(req.Keyword.ToLower())  //Contains มีอักษรบางส่วน
                || e.Code!.ToLower().Contains(req.Keyword.ToLower()));
            }
            var coupon = await query.Skip(skipRecords).Take(pageSize).Select(d => new GetCouponsDTO
            {
                CouponId = d.Id,
                CouponName = d.Name,
                Description = d.Description,
                CouponCode = d.Code,
                Amount = d.Amount,
                UsedAmount = d.UsedCoupons.Count,
                StartTime = TimeZoneInfo.ConvertTimeFromUtc(d.StartTimeUTC, localeTimeZone),
                EndTime = TimeZoneInfo.ConvertTimeFromUtc(d.EndTimeUTC, localeTimeZone),
                DiscountRate = d.Discount,
                MaxDiscount = d.MaxDiscount,
                MinimumPrice = d.MinimumPrice,
                IsDiscountPercent = d.IsDiscountPercent,
                IsCouponAvailable = d.IsAvailable
            }).ToListAsync();
            var res = new PagingDTO<GetCouponsDTO>
            {
                TotalRecords = totalRecords,
                Items = coupon
            };
            return Ok(res);
        }

        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }
    [HttpGet("Used")]
    [Authorize]
    public async Task<IActionResult> GetUsedCoupon([FromQuery] GetCouponByOrderDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var usedCoupon = await _appDbContext.UsedCoupons.Where(uc => uc.UserId == user.Id && uc.OrderId == req.OrderId).Include(uc => uc.Coupon).Select(uc => new GetUsedCouponDTO
            {
                CouponId = uc.CouponId,
                CouponCode = uc.Coupon!.Code,
                CouponName = uc.Coupon.Name,
                Description = uc.Coupon.Description,
                DiscountRate = uc.Coupon.Discount,
                IsDiscountPercent = uc.Coupon.IsDiscountPercent,
                MaxDiscount = uc.Coupon.MaxDiscount,
                MinimumPrice = uc.Coupon.MinimumPrice,
            }).ToListAsync();
            return Ok(usedCoupon);
        }

        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPost("Order/{id}")]
    [Authorize]
    public async Task<IActionResult> UseCoupon(Guid id, UseCouponCodeDTO req)
    {
        using var transaction = await _appDbContext.Database.BeginTransactionAsync();
        try
        {

            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }


            var curCoupon = await _appDbContext.Coupons.Include(uc => uc.UsedCoupons).FirstOrDefaultAsync(c => c.Code == req.CouponCode);
            if (curCoupon == null) return NotFound("ไม่มีคูปองนี้");
            if (curCoupon.UsedCoupons.Any(uc => uc.OrderId == id && uc.UserId == user.Id))
            {
                var errors = new[] { "คุณใช้คูปองนี้ไปแล้ว" };
                return BadRequest(new { Errors = errors });
            }
            if (curCoupon.Amount <= curCoupon.UsedCoupons.Count)
            {
                var errors = new[] { "คูปองถูกใช้หมดแล้ว" };
                return BadRequest(new { Errors = errors });
            }
            if (curCoupon.IsAvailable == false)
            {
                var errors = new[] { "คูปองไม่สามารถใช้งานได้แล้ว" };
                return BadRequest(new { Errors = errors });
            }

            //เช็คว่า Order นี้ ใช้คูปองไปแล้วหรือยัง 1 Order 1 คูปอง
            var isOrderUseCoupon = await _appDbContext.UsedCoupons.AnyAsync(uc => uc.OrderId == id);
            if (isOrderUseCoupon)
            {
                var errors = new[] { "รายการนี้ได้ใช้คูปองไปแล้ว" };
                return BadRequest(new { Errors = errors });
            }

            if (curCoupon.StartTimeUTC > DateTime.UtcNow)
            {
                var errors = new[] { "คูปองยังไม่ถึงเวลาใช้งาน" };
                return BadRequest(new { Errors = errors });
            }

            if (curCoupon.EndTimeUTC < DateTime.UtcNow)
            {
                var errors = new[] { "คูปองหมดอายุแล้ว" };
                return BadRequest(new { Errors = errors });
            }

            var targetOrder = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (targetOrder == null) return NotFound();

            if (targetOrder.TotalPrice < curCoupon.MinimumPrice && curCoupon.MinimumPrice != 0)
            {
                var errors = new[] { $"ราคาขั้นต่ำที่สามารถใช้คูปองได้: {curCoupon.MinimumPrice} บาท" };
                return BadRequest(new { Errors = errors });
            }
            targetOrder.UsedCoupon = true;
            targetOrder.NetPrice = _priceCalculate.DiscountPrice(targetOrder.NetPrice, curCoupon.Discount, curCoupon.IsDiscountPercent, curCoupon.MaxDiscount);
            var useCoupon = new UsedCouponModel
            {
                CouponId = curCoupon.Id,
                OrderId = targetOrder.Id,
                UserId = user.Id,
            };
            _appDbContext.UsedCoupons.Add(useCoupon);
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync(); //ถ้าไม่เกิดerror ระหว่างทาง ค่อยยืนยันการเปลี่ยนแปลง

            return Ok(targetOrder.NetPrice);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(); //ถ้าเกิด error จะย้อนกลับค่าทุกอย่างที่ track ไว้จาก  BeginTransactionAsync
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpDelete("Order/{id}")]
    [Authorize]
    public async Task<IActionResult> CancelUsedCoupon(Guid id)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var usedCoupon = await _appDbContext.UsedCoupons.Include(uc => uc.Order).FirstOrDefaultAsync(uc => uc.OrderId == id);
            if (usedCoupon == null) return NotFound();
            usedCoupon.Order!.NetPrice = usedCoupon.Order!.TotalPrice + usedCoupon.Order!.TransportPrice;  //ปรับราคากลับเป็นเท่าเดิม
            usedCoupon.Order!.UsedCoupon = false;
            _appDbContext.Remove(usedCoupon);
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
    public async Task<IActionResult> RemoveCoupon(Guid id)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var targetCoupon = await _appDbContext.Coupons.FirstOrDefaultAsync(c => c.Id == id);
            if (targetCoupon == null) return NotFound();
            _appDbContext.Coupons.Remove(targetCoupon);
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
    public async Task<IActionResult> UpdateCoupon(Guid id, UpdateCouponDTO req)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var targetCoupon = await _appDbContext.Coupons.FirstOrDefaultAsync(c => c.Id == id);
            if (targetCoupon == null) return NotFound();
            targetCoupon.Name = req.CouponName;
            targetCoupon.Code = req.CouponCode;
            targetCoupon.Description = req.Description;
            targetCoupon.StartTimeUTC = req.StartTime;
            targetCoupon.EndTimeUTC = req.EndTime;
            targetCoupon.Amount = req.Amount;
            targetCoupon.Discount = req.DiscountRate;
            targetCoupon.IsDiscountPercent = req.IsDiscountPercent;
            targetCoupon.MaxDiscount = req.MaxDiscount;
            targetCoupon.MinimumPrice = req.MinimumPrice;

            _appDbContext.Coupons.Update(targetCoupon);
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCouponAvailable(Guid id, UpdateCouponAvailableDTO req)
    {
        try
        {
            var curUserId = User.FindFirstValue("uid");
            var user = await _userManager.FindByIdAsync(curUserId!);
            var curCoupon = await _appDbContext.Coupons.FirstOrDefaultAsync(x => x.Id == id);
            if (curCoupon == null) return NotFound();
            curCoupon.IsAvailable = req.IsAvailable;
            _appDbContext.Coupons.Update(curCoupon);
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
    [Authorize(Roles = "Admin")]

    public async Task<IActionResult> DeleteSelectedProduct(DeleteSelectedCouponDTO req)
    {
        try
        {
            if (req.SelectedCouponId == null || !req.SelectedCouponId.Any())
            {
                return NotFound();
            }
            // ดึงสินค้าที่ต้องการลบทั้งหมด
            var selectedCoupons = await _appDbContext.Coupons
                .Where(p => req.SelectedCouponId.Contains(p.Id))
                .ToListAsync();

            if (!selectedCoupons.Any())
            {
                return NotFound();
            }
            // ลบสินค้าที่ดึงมา
            _appDbContext.Coupons.RemoveRange(selectedCoupons);
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

