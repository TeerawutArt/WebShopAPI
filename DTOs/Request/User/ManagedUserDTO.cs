using System;

namespace WebShoppingAPI.DTOs.Request;

public class ManageUserDTO
{
    public string? TargetUserId { get; set; }
    public bool Blocked { get; set; }


}
