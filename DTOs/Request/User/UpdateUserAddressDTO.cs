using System;

namespace WebShoppingAPI.DTOs.Request.User;

public class UpdateUserAddressDTO
{
    public string? AddressName { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhoneNumber { get; set; }
    public string? AddressInfo { get; set; }
    public bool IsDefault { get; set; }
}
