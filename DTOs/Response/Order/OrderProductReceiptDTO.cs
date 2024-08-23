using System;

namespace WebShoppingAPI.DTOs.Response;

public class OrderProductReceiptDTO
{

    public Guid Id { get; set; }

    public string? CreatedBy { get; set; }
    public string? ReceiptImageURL { get; set; }
    public DateTime UploadTime { get; set; }
    public DateTime VerifiedTime { get; set; }
    public bool Completed { get; set; }
    public string? VerifiedBy { get; set; }


    public Guid OrderProductId { get; set; }


}
