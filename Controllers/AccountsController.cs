
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
namespace WebShoppingAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class AccountsController(UserManager<UserModel> userManager, TokenHelper tokenHelper) : ControllerBase
{
    //dependency injection
    private readonly UserManager<UserModel> _userManager = userManager;
    private readonly TokenHelper _tokenHelper = tokenHelper;

    private readonly TimeZoneInfo localeTimeZone = TimeZoneInfo.Local;
    private readonly string defaultImageURLForUser = "default-user-image.png";

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterUser(RegisterUserDTO req)
    {
        //แปลงเวลา locate เป็น UTC
        /* DateTime utcBirthDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(req.BirthDate!), localeTimeZone); */ //วันเกิดเก็บเวลา locale ก็ได้ (เวลาไม่มีผล)

        var newUser = new UserModel
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            UserName = req.UserName,
            PhoneNumber = req.PhoneNumber,
            Gender = req.Gender,
            BirthDate = DateTime.Parse(req.BirthDate!),
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
                Gender = user.Gender,
                BirthDate = user.BirthDate,
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
