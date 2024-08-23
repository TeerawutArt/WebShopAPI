using System;

namespace WebShoppingAPI.DTOs.Response.User;

public class AddressDTO
{
    public Guid AddressId { get; set; }
    public string? AddressName { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhoneNumber { get; set; }
    public string? AddressInfo { get; set; }

}
