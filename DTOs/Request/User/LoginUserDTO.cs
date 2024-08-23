using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request;

public class LoginUserDTO
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    public required string UserName { get; set; }
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    public required string Password { get; set; }

}
