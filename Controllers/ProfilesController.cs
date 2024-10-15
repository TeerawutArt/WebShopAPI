using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Request.User;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.DTOs.Response.User;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Controllers;

[Route("Accounts/[controller]")]
[ApiController]
public class ProfilesController(UserManager<UserModel> userManager, AppDbContext appDbContext,
FileService fileService, IConfiguration iConfiguration) : ControllerBase
{
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly IConfiguration _iConfiguration = iConfiguration;
    private readonly string defaultImageURLForUser = "default-user-image.png";
    private readonly FileService _fileService = fileService;
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirstValue("uid");
        UserModel? user = await _userManager.FindByIdAsync(userId!);
        if (user is null)
        {
            var errors = new[] { "Invalid request." };
            return BadRequest(new { Errors = errors });
        }
        var curUser = new UserProfileDTO
        {
            UserImageURL = user.UserImageURL,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            Blocked = user.Blocked,
            //วิธี Projection ( SelectMany เอามาแค่ที่ต้องการ Include เอามาหมด ในแง่ของการดึงข้อมูลมาบางส่วน) 
            Addresses = _userManager.Users.Where(u => u.Id == user.Id).SelectMany(a => a.Addresses).Select(a => new AddressDTO
            {
                AddressId = a.Id,
                AddressName = a.Name,
                ReceiverName = a.Receiver,
                ReceiverPhoneNumber = a.PhoneNumber,
                AddressInfo = a.AddressInfo,
                IsDefault = a.IsDefault
            }).ToList(),

        };
        return Ok(curUser);
    }

    [HttpPut("Update")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateUser(UpdateUserDTO req)
    {
        var userId = User.FindFirstValue("uid");
        UserModel? user = await _userManager.FindByIdAsync(userId!);
        if (user is null)
        {
            var errors = new[] { "Invalid request or no permission" };
            return BadRequest(new { Errors = errors });
        }

        if (req.UserImage is not null)
        {
            try
            {
                if (req.UserImage!.Length > 1 * 1024 * 1024) return StatusCode(StatusCodes.Status400BadRequest, "ขนาดไฟล์รูปต้องไม่ใหญ่เกิน 1 MB");
                string[] allowFileExtensions = [".jpg", ".jpeg", ".png"];
                string createdImageName = await _fileService.SaveFileAsync(req.UserImage!, allowFileExtensions);
                //ลบรูปเก่าที่อยู่ในโฟล์เดอร์ "UploadImage" ยกเว้นรูปตั้งต้น
                if (user.UserImageURL != defaultImageURLForUser) _fileService.DeleteFile(user.UserImageURL!);
                //อัปเดทข้อมูล user

                user.Email = req.Email;
                user.FirstName = req.FirstName;
                user.LastName = req.LastName;
                user.BirthDate = DateTime.Parse(req.BirthDate!);
                user.PhoneNumber = req.PhoneNumber;
                user.Gender = req.Gender;
                user.UserImageURL = createdImageName;
            }
            catch (Exception ex)
            {
                var errors = new[] { ex.Message };
                return BadRequest(new { Errors = errors });
            }
        }
        else
        {
            user.Email = req.Email;
            user.FirstName = req.FirstName;
            user.LastName = req.LastName;
            user.BirthDate = DateTime.Parse(req.BirthDate!);
            user.PhoneNumber = req.PhoneNumber;
            user.Gender = req.Gender;
        }
        await _userManager.UpdateAsync(user);
        return NoContent();
    }




    [HttpPost("Address")]
    [Authorize]

    public async Task<IActionResult> CreateUserAddress(CreateUserAddressDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            int maxAllowAddress = _iConfiguration.GetValue<int>("MaxAllowAddress"); //จำนวนที่อยู่สูงสุดที่ให้สร้างได้
            var addressCount = _appDbContext.Addresses.Count(a => a.UserId == user.Id);
            if (addressCount >= maxAllowAddress)
            {
                var errors = new[] { "ที่อยู่ที่สร้างได้ต้องไม่เกิน " + maxAllowAddress };
                return BadRequest(new { Errors = errors });
            }
            var newAddress = new AddressModel
            {
                Name = req.AddressName,
                Receiver = req.ReceiverName,
                PhoneNumber = req.ReceiverPhoneNumber,
                AddressInfo = req.AddressInfo,
                UserId = user.Id,
                IsDefault = false,
            };
            if (addressCount == 0) //ยังไม่มีที่อยู่
            { newAddress.IsDefault = true; }
            _appDbContext.Addresses.Add(newAddress);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }
    [HttpGet("Address")]
    [Authorize]

    public async Task<IActionResult> GetUserAddress()
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var address = _appDbContext.Addresses.Where(a => a.UserId == user.Id).Select(a => new AddressDTO
            {
                AddressId = a.Id,
                AddressName = a.Name,
                ReceiverName = a.Receiver,
                ReceiverPhoneNumber = a.PhoneNumber,
                AddressInfo = a.AddressInfo,
                IsDefault = a.IsDefault

            }).ToList();
            return Ok(address);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }


    [HttpPut("Address/{id}")]
    [Authorize]

    public async Task<IActionResult> UpdateUserAddress(Guid id, UpdateUserAddressDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var curAddress = await _appDbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == user.Id && a.Id == id);
            if (curAddress == null) return NotFound();
            curAddress.Name = req.AddressName;
            curAddress.Receiver = req.ReceiverName;
            curAddress.PhoneNumber = req.ReceiverPhoneNumber;
            curAddress.AddressInfo = req.AddressInfo;
            _appDbContext.Addresses.Update(curAddress);
            await _appDbContext.SaveChangesAsync();
            return NoContent();

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpPut("Address/Default/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAddressDefault(Guid id, UpdateDefaultAddressDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var curAddress = await _appDbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == user.Id && a.Id == id);
            if (curAddress == null) return NotFound();
            curAddress.IsDefault = req.IsDefault;
            _appDbContext.Addresses.Update(curAddress);
            await _appDbContext.SaveChangesAsync();
            if (req.IsDefault == true)
            {
                var otherAddress = await _appDbContext.Addresses.Where(a => a.UserId == user.Id && a.Id != id).ToListAsync();
                if (otherAddress.Count != 0)
                {
                    foreach (var address in otherAddress)
                    {
                        address.IsDefault = false;
                        //เปลี่ยน IsDefault ที่อยู่อื่นเป็น false
                    }
                    _appDbContext.Addresses.UpdateRange(otherAddress);
                    await _appDbContext.SaveChangesAsync();
                }
            }

            return NoContent();

        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpDelete("Address/{id}")]
    [Authorize]

    public async Task<IActionResult> RemoveUserAddress(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            if (user is null)
            {
                var errors = new[] { "Invalid request or no permission" };
                return BadRequest(new { Errors = errors });
            }
            var curAddress = await _appDbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == user.Id && a.Id == id);
            if (curAddress == null) return NotFound();
            _appDbContext.Addresses.Remove(curAddress);
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

