using WebShoppingAPI.DTOs.Response.User;

namespace WebShoppingAPI.DTOs.Response;

public class UserProfileDTO
{
    public string? UserImageURL { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime BirthDate { get; set; }

    public string? Email { get; set; }
    public bool Blocked { get; set; }

    public List<AddressDTO> Addresses { get; set; } = new List<AddressDTO>();


}
