
using System.Security.Claims;
using WebShoppingAPI.DTOs.Request;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.Helpers;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Response.User;
using WebShoppingAPI.DTOs.Request.User;
using Microsoft.Extensions.Options;
namespace WebShoppingAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class AccountsController(UserManager<UserModel> userManager, TokenHelper tokenHelper,
FileService fileService, IConfiguration iConfiguration, AppDbContext appDbContext) : ControllerBase
{
    //dependency injection
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly TokenHelper _tokenHelper = tokenHelper;
    private readonly FileService _fileService = fileService;
    private readonly IConfiguration _iConfiguration = iConfiguration;
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly string defaultImageURLForUser = "default-user-image.png";

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterUser(RegisterUserDTO req)
    {
        var newUser = new UserModel
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            UserName = req.UserName,
            PhoneNumber = req.PhoneNumber,
            Blocked = false,
            UserImageURL = defaultImageURLForUser,

        };

        var result = await _userManager.CreateAsync(newUser, req.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { Errors = errors });
        }
        //ทุก user เมื่อสร้างใหม่ จะเป็น role Customer ก่อนเสมอ
        try
        {
            await _userManager.AddToRoleAsync(newUser, "Customer");
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(newUser); //ถ้าเกิดการผิดพลาดให้ลบ user ออกไป
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
        return StatusCode(StatusCodes.Status201Created);

    }

    [HttpPost("Login")]
    public async Task<IActionResult> LoginUser(LoginUserDTO req)
    {
        var user = await _userManager.FindByNameAsync(req.UserName);


        if (user is null || !await _userManager.CheckPasswordAsync(user, req.Password))
        {
            var errors = new[] { "ชื่อผู้ใช้งาน หรือ รหัสผ่าน ไม่ถูกต้อง" };
            return Unauthorized(new { Errors = errors });
        }
        if (user.Blocked)
        {

            var errors = new[] { "คุณถูกระงับการใช้งาน" };
            return Unauthorized(new { Errors = errors });
        }
        var token = await _tokenHelper.CreateToken(user);
        return Ok(new TokenResultDTO { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken });
    }

    [HttpPost("Token/Refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDTO request)
    {
        string? username;
        try
        {
            var claimsPrincipal = _tokenHelper.GetClaimsPrincipalFromExpiredToken(request.AccessToken!);
            username = claimsPrincipal.FindFirstValue("preferred_username");
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return Unauthorized(new { Errors = errors });
        }

        var user = await _userManager.FindByNameAsync(username!);

        if (user is null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            var errors = new[] { "Invalid token." };
            return Unauthorized(new { Errors = errors });
        }

        var token = await _tokenHelper.CreateToken(user, false);

        return Ok(new TokenResultDTO { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken });
    }

    //logout
    [HttpPost("Token/Revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] object request)
    {
        var userId = User.FindFirstValue("uid");

        var user = await _userManager.FindByIdAsync(userId!);

        if (user is null)
        {
            var errors = new[] { "Invalid revoke request." };
            return BadRequest(new { Errors = errors });
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _userManager.UpdateAsync(user);

        return NoContent();
    }
    [HttpGet("Profile")]
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
            Blocked = user.Blocked,
            //วิธี Projection ( SelectMany เอามาแค่ที่ต้องการ Include เอามาหมด ในแง่ของการดึงข้อมูลมาบางส่วน) 
            Addresses = _userManager.Users.Where(u => u.Id == user.Id).SelectMany(a => a.Addresses).Select(a => new AddressDTO
            {
                AddressId = a.Id,
                AddressName = a.Name,
                ReceiverName = a.Receiver,
                ReceiverPhoneNumber = a.PhoneNumber,
                AddressInfo = a.AddressInfo,
            }).ToList(),

        };
        return Ok(curUser);
    }

    [HttpPut("Profile/Update")]
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
                user.PhoneNumber = req.PhoneNumber;
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
            user.PhoneNumber = req.PhoneNumber;
        }
        await _userManager.UpdateAsync(user);
        return NoContent();
    }




    [HttpPost("Profile/Address")]
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
                UserId = user.Id
            };
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
    [HttpGet("Profile/Address")]
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
                AddressInfo = a.AddressInfo
            }).ToList();
            return Ok(address);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }

    }


    [HttpPut("Profile/Address/{id}")]
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

    [HttpDelete("Profile/Address{id}")]
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


    [HttpPut("UserManage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BlockedUser(ManageUserDTO req)
    {
        try
        {
            UserModel? targetUser = await _userManager.FindByIdAsync(req.TargetUserId!);
            var userRole = await _userManager.GetRolesAsync(targetUser!);
            if (userRole.Any(role => role == "Admin") && req.Blocked == true)
            {
                var errors = new[] { "ไม่สามารถระงับผู้ใช้งานที่เป็น Admin ได้" };
                return BadRequest(new { Errors = errors });
            }
            if (targetUser == null) return NotFound();
            targetUser.Blocked = req.Blocked;
            await _userManager.UpdateAsync(targetUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }
    [HttpGet("AllUser")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> getAllUser()
    {
        try
        {
            List<AllUserDTO> users = await _userManager.Users.Select(user => new AllUserDTO
            {
                UserID = user.Id,
                UserImageURL = user.UserImageURL,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Addresses = user.Addresses.Select(a => new AddressDTO
                {
                    AddressId = a.Id,
                    AddressName = a.Name,
                    ReceiverName = a.Receiver,
                    ReceiverPhoneNumber = a.PhoneNumber,
                    AddressInfo = a.AddressInfo,
                }).ToList(),
                Blocked = user.Blocked,
            }).ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

}
