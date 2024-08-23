
using Microsoft.AspNetCore.Identity;

namespace WebShoppingAPI.Models;

public class RoleModel : IdentityRole<Guid>

{
    public string? Description { get; set; }


}
