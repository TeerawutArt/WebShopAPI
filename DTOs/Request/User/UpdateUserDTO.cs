
using System.ComponentModel.DataAnnotations;


namespace WebShoppingAPI.DTOs.Request;

public class UpdateUserDTO
{

    public IFormFile? UserImage { get; set; }


    [Required(ErrorMessage = "กรุณากรอกชื่อจริง")]
    public required string FirstName { get; set; }
    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    public required string LastName { get; set; }


    [Required(ErrorMessage = "กรุณากรอกอีเมล"), EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public required string Email { get; set; }
    public string? BirthDate { get; set; } //ส่งมาเป็น string เพราะเรื่องของ multiform ค่อยไปแปลงเอา

    [Required(ErrorMessage = "กรุณาเบอร์โทรศัพท์")]
    public required string PhoneNumber { get; set; }
    [Required(ErrorMessage = "กรุณาระบุเพศ")]
    public required string Gender { get; set; }

}
