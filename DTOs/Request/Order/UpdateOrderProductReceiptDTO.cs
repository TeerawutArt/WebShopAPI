using System;

namespace WebShoppingAPI.DTOs.Request;

public class UpdateOrderProductReceiptDTO
{
    public IFormFile? ReceiptImage { get; set; }

}
