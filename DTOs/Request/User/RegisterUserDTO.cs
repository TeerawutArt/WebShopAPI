using System.ComponentModel.DataAnnotations;

namespace WebShoppingAPI.DTOs.Request;

public class RegisterUserDTO
{



    [Required(ErrorMessage = "กรุณากรอกชื่อจริง")]
    [MaxLength(500, ErrorMessage = "ข้อความยาวเกินไป")]
    public required string FirstName { get; set; }
    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    [MaxLength(500, ErrorMessage = "ข้อความยาวเกินไป")]
    public required string LastName { get; set; }
    [Required(ErrorMessage = "กรุณากรอกอีเมล"), EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [MaxLength(500, ErrorMessage = "ข้อความยาวเกินไป")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้งาน"),
    MinLength(5, ErrorMessage = "ชื่อผู้ใช้งานต้องมีความยาวมากกว่า 5 ตัวอักษร"),
    MaxLength(20, ErrorMessage = "ชื่อผู้ใช้งานต้องไม่เกิน 20 ตัวอักษร")]
    public required string UserName { get; set; }
    [Required(ErrorMessage = "กรุณาเบอร์โทรศัพท์")]
    [MaxLength(20, ErrorMessage = "ข้อความยาวเกินไป")]
    public required string PhoneNumber { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเพศ")]
    public required string Gender { get; set; }
    [Required(ErrorMessage = "กรุณาระบุวันเกิด")]
    public string? BirthDate { get; set; } //ส่งมาเป็น string เพราะเรื่องของ multiform ค่อยไปแปลงเอา
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน"),
    MinLength(5, ErrorMessage = "รหัสผ่านต้องมีความยาวมากกว่า 5 ตัวอักษร"),
    MaxLength(20, ErrorMessage = "รหัสผ่านต้องไม่เกิน 20 ตัวอักษร")]
    public required string Password { get; set; }
    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน"), Compare(nameof(Password), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    public required string ConfirmPassword { get; set; }



}
