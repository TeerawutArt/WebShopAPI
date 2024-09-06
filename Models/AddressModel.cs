using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShoppingAPI.Models;

public class AddressModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Receiver { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressInfo { get; set; }
    public bool IsDefault { get; set; }
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? Users { get; set; }
}
