using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Request.Order;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.DTOs.Response.Order;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class OrdersController(AppDbContext appDbContext, UserManager<UserModel> userManager, IConfiguration iConfiguration) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly IConfiguration _iConfiguration = iConfiguration;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder(CreateOrderProductDTO[] req)
    {
        try
        {
            //logic คือจะสร้าง Order เมื่อมีการกดสั่งซื้อ 
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            double ExpiryInDay = _iConfiguration.GetValue<double>("OrderExpiredInDay"); //จำนวนวันที่Order จะหมดอายุ
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var newOrder = new OrderModel
            {
                OrderTime = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddDays(ExpiryInDay),
                Status = "รอชำระเงิน",
                UserId = user.Id,
                TransportInfo = "",
                IsPaid = false
            };
            //save เพื่อ gen Order Id
            _appDbContext.Orders.Add(newOrder);
            await _appDbContext.SaveChangesAsync();
            //สร้าง OrderProduct แล้วเอามาใส่ใน Order
            double totalPrice = 0;
            List<string> insufficientProductsName = new(); // เก็บชื่อสินค้าที่จำนวนไม่พอ
            foreach (var item in req)
            {
                // ค้นหาสินค้า
                var product = await _appDbContext.Products.Include(p => p.Discount).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product is null)
                {
                    var errors = new[] { "ไม่มีสินค้านี้" };
                    return BadRequest(new { Errors = errors });
                }

                // ตรวจสอบว่ามีสินค้าพอไหม
                if (product.TotalAmount < item.ProductQuantity)
                {
                    //ถ้าไม่พอให้เพิ่มชื่อสินค้าที่ไม่พอ และข้ามloopนี้
                    insufficientProductsName.Add(product.Name);
                    continue; // ข้ามการสร้าง OrderProduct สำหรับสินค้านี้
                }
                //ตรวจสอบว่าสินค้าอยู่ในช่วงลดราคาไหมและมีการลดราคาไหม 
                if (product.Discount != null)
                {
                    DateTime curDate = DateTime.UtcNow;
                    var discount = product.Discount;
                    if (curDate >= discount.StartTime && curDate < discount.EndTime && discount.IsDiscounted)
                    {
                        var newOrderProduct = new OrderProductModel
                        {
                            OrderId = newOrder.Id,
                            ProductId = item.ProductId,
                            Quantity = item.ProductQuantity,
                            UnitPrice = product.DiscountPrice,
                            NetPrice = product.DiscountPrice * item.ProductQuantity
                        };
                        totalPrice += product.DiscountPrice * item.ProductQuantity;

                        _appDbContext.OrderProducts.Add(newOrderProduct);
                    }
                    else //หมดช่วงเวลาลดราคา หรือปิดการลดราคาไปแล้ว
                    {
                        var newOrderProduct = new OrderProductModel
                        {
                            OrderId = newOrder.Id,
                            ProductId = item.ProductId,
                            Quantity = item.ProductQuantity,
                            UnitPrice = product.Price,
                            NetPrice = product.Price * item.ProductQuantity
                        };
                        totalPrice += product.Price * item.ProductQuantity;
                        _appDbContext.OrderProducts.Add(newOrderProduct);
                    }
                }
                else //ไม่ได้ลดราคา
                {
                    var newOrderProduct = new OrderProductModel
                    {
                        OrderId = newOrder.Id,
                        ProductId = item.ProductId,
                        Quantity = item.ProductQuantity,
                        UnitPrice = product.Price,
                        NetPrice = product.Price * item.ProductQuantity
                    };
                    totalPrice += product.Price * item.ProductQuantity;

                    _appDbContext.OrderProducts.Add(newOrderProduct);
                }
            }
            // ถ้ามีสินค้าที่จำนวนไม่พอ ส่งชื่อสินค้าออกไป
            if (insufficientProductsName.Any())
            {
                var errors = new[] { $"รายการสินค้าที่มีจำนวนไม่เพียงพอมีดังนี้: {string.Join(", ", insufficientProductsName)}" };
                return BadRequest(new { Errors = errors });
            }
            //update totalPrice ให้ Order
            var curOrder = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == newOrder.Id);
            curOrder!.TotalPrice = totalPrice;
            _appDbContext.Orders.Update(curOrder);
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
    public async Task<IActionResult> GetOrders()
    {
        UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
        if (user is null)
        {
            var errors = new[] { "Invalid request or no permission" };
            return BadRequest(new { Errors = errors });
        }
        var userRole = await _userManager.GetRolesAsync(user);
        if (userRole.Any(role => role == "Customer"))
        {
            var order = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.UserId == user.Id);
            if (order is null) return NotFound();
            var curOrder = new OrderDTO
            {
                OrderTime = order.OrderTime,
                ExpiryTime = order.ExpiryTime,
                TransactionTime = order.TransactionTime,
                IsSuccess = order.IsPaid,
                Status = order.Status,
                TransportInfo = order.TransportInfo,
                OrderProducts = _appDbContext.Orders.Where(o => o.UserId == user.Id).SelectMany(o => o.OrderProducts).Select(p => new OrderProductDTO
                {
                    ProductName = p.Product!.Name,
                    UnitPrice = p.UnitPrice,
                    ProductQuantity = p.Quantity,
                    NetPrice = p.NetPrice,
                }
                 ).ToList(),
                TotalPrice = order.TotalPrice,
            };
            return Ok(curOrder);
        }
        if (userRole.Any(role => role == "Admin") || userRole.Any(role => role == "Sale"))
        {
            //staff or admin role ShowAllOrder
            List<OrderDTO> orders = await _appDbContext.Orders.Select(order => new OrderDTO
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OrderTime = order.OrderTime,
                ExpiryTime = order.ExpiryTime,
                TransactionTime = order.TransactionTime,
                IsSuccess = order.IsPaid,
                Status = order.Status,
                TransportInfo = order.TransportInfo,
                TotalPrice = order.TotalPrice,
            }).ToListAsync();
            return Ok(orders);
        }
        else
        {//ดักไว้
            var errors = new[] { "Invalid request or no permission" };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPost("Confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmOrder(ConfirmOrderDTO req, bool transaction)
    {
        try
        {
            var curOrder = await _appDbContext.Orders.Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product).FirstOrDefaultAsync(o => o.Id == req.OrderId);
            if (curOrder == null)
            {
                var errors = new[] { "ไม่พบ Order ที่ชำระเงินกรุณาติดต่อผู้ดูแลระบบ" };
                return BadRequest(new { Errors = errors });
            }
            if (curOrder.ExpiryTime > DateTime.UtcNow)
            {
                curOrder.Status = "เลยกำหนดชำระเงิน";
                var errors = new[] { "คำสั่งซื้อถูกยกเลิกเนื่องจากเลยเวลาชำระเงิน" }; //ตรงนี้ให้หน้าบ้านฟ้องสาเหตุ :คำสั่งซื้อถูกยกเลิกเนื่องจากเลยเวลาชำระเงิน
                return BadRequest(new { Errors = errors });
            }
            //ตรงนี้ตรวจว่ายังมีสินค้าพอที่จะซื้อหรือเปล่า (ตรวจมันหลายๆรอบนี่หละ)
            List<string> insufficientProductsName = new(); // เก็บชื่อสินค้าที่จำนวนไม่พอ
            foreach (var orderProduct in curOrder.OrderProducts)
            {
                var product = orderProduct.Product;
                if (product != null)
                {
                    if (product.TotalAmount < orderProduct.Quantity)
                    {
                        insufficientProductsName.Add(product.Name);
                    }
                }
            }
            // ถ้ามีสินค้าที่จำนวนไม่พอ ส่งชื่อสินค้าออกไป
            if (insufficientProductsName.Any())
            {
                var errors = new[] { $"รายการสินค้าที่มีจำนวนไม่เพียงพอมีดังนี้: {string.Join(", ", insufficientProductsName)}" };
                return BadRequest(new { Errors = errors });
            }
            //ผ่านทุกเงื่อนไข ก็มาเช็คการทำธุรกรรมต่อ 
            if (transaction)
            {
                curOrder!.IsPaid = true;
                curOrder!.Status = "อยู่ระหว่างจัดส่ง";
                curOrder!.TransactionTime = DateTime.UtcNow;

                foreach (var orderProduct in curOrder.OrderProducts)
                {
                    var product = orderProduct.Product;
                    if (product != null)
                    {
                        //เพื่มจำนวนที่ขายได้
                        product.SoldAmount += orderProduct.Quantity;
                        // ลดจำนวนสินค้า (สาเหตุที่ลดตรงนี้ไม่ใช่ตอนสั่งOrder เพราะกันพวกกั๊กของไว้ไม่ยอมจ่ายเงิน)
                        product.TotalAmount -= orderProduct.Quantity;
                        //เปลี่ยนแปลงสถานะสินค้าถ้าของหมด
                        product.IsAvailable = product.TotalAmount <= 0;
                    }
                }
                //ไม่ต้องอัปเดท เพราะเมื่อใช้ Include EF จะTrack การเปลี่ยนแปลงและอัปเดทอัตโนมัติเมื่อ save
                await _appDbContext.SaveChangesAsync();
                return Ok(true);
            }
            else
            {
                //ถ้ายังไม่ได้ชำระเงิน
                var errors = new[] { "กรุณาชำระเงิน" };
                return BadRequest(new { Errors = errors });
            }
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }
    [HttpPut("Detail/{id}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateOrder(Guid id, UpdateOrderProductDTO req)
    {
        try
        {

            var targetOrder = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (targetOrder == null) return NotFound();
            targetOrder.Status = req.OrderStatus;
            targetOrder.TransportInfo = req.OrderTransportInfo;
            _appDbContext.Orders.Update(targetOrder);
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
    [Authorize]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        try
        {
            UserModel? user = await _userManager.FindByIdAsync(User.FindFirstValue("uid")!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var targetOrder = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (targetOrder == null) return NotFound();
            _appDbContext.Orders.Remove(targetOrder);
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

