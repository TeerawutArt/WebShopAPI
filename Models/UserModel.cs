using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;

namespace WebShoppingAPI.Models;

public class UserModel : IdentityUser<Guid>
{

    public string? UserImageURL { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public ICollection<AddressModel> Addresses { get; set; } = new List<AddressModel>();
    //จริงประสิทธิภาพของ empty list กับ Null มันน้อยมาก ถ้าไม่คิดมากก็เซ็ตเป็น empty list หมดเลยก็ได้

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; } //null

    public bool Blocked { get; set; }
    public ICollection<OrderModel>? Orders { get; set; } //null
    public CartModel? Cart { get; set; }

}
