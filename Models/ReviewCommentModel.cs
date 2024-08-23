﻿
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebShoppingAPI.Models;

namespace WebShoppingAPI.Models;

public class ReviewCommentModel
{

    [Key]
    public Guid Id { get; set; }


    public Guid ReplyCommentId { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public double Score { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime UpdateTime { get; set; }


    //many-to-one
    [ForeignKey("ProductModel")]
    public Guid ProductId { get; set; }
    public ProductModel? Product { get; set; }
    //one-to-one
    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public UserModel? Users { get; set; }
}
