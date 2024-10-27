/* using System.Security.Claims;
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShoppingAPI.DTOs.Response;
using WebShoppingAPI.DTOs.Request;

namespace WebShoppingAPI.Controllers;

[ApiController]
[Route("[Controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class ReviewCommentsController(AppDbContext appDbContext, UserManager<UserModel> userManager) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly UserManager<UserModel> _userManager = userManager;


    [HttpGet]
    public async Task<IActionResult> GetReviewComments()
    {
        try
        {
            var reviewComments = await _appDbContext.ReviewComments.OrderByDescending(c => c.CreatedTimeUTC).Select(c => new ReviewCommentDTO
            {
                ReviewCommentId = c.Id,
                ProductId = c.ProductId,
                CreatedBy = c.CreatedBy,
                Title = c.Title,
                Content = c.Content,
                Score = c.Score,
                CreatedTime = c.CreatedTimeUTC,
                UpdateTime = c.UpdateTimeUTC,
            }).ToListAsync();

            return Ok(reviewComments);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }


    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetReviewCommentByProduct(Guid id)
    {
        try
        {
            var curProduct = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (curProduct == null) return NotFound();
            var query = _appDbContext.ReviewComments.AsQueryable();
            query = query.Where(c => c.ProductId == curProduct.Id);
            var filterReviewComment = await query.OrderByDescending(q => q.CreatedTimeUTC).Select(x => new ReviewCommentDTO
            {
                ReviewCommentId = x.Id,
                CreatedBy = x.CreatedBy,
                Title = x.Title,
                Score = x.Score,
                Content = x.Content,
                CreatedTime = x.CreatedTimeUTC,
                UpdateTime = x.UpdateTimeUTC,
            }).ToListAsync();

            return Ok(filterReviewComment);
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReviewComment(CreateReviewCommentDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            var curProduct = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == req.ProductId);
            if (curProduct == null) return NotFound();
            var newReviewComment = new ReviewCommentModel
            {

                CreatedBy = User.FindFirstValue("name"),
                UserId = user!.Id,
                Title = req.Title,
                Score = req.Score,
                Content = req.Content,
                ProductId = req.ProductId,
                CreatedTimeUTC = DateTime.UtcNow
            };
            _appDbContext.Add(newReviewComment);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpPost("Reply/{id}")]
    [Authorize]
    public async Task<IActionResult> ReplyReviewComment(Guid id, CreateReviewCommentDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            //ตรวจสอบ Product ,ReviewComment ที่ตอบกลับ
            var curReviewComment = await _appDbContext.Products.FirstOrDefaultAsync(x => x.Id == req.ProductId && x.ReviewComments!.Any(c => c.Id == id));
            if (curReviewComment == null) return NotFound();
            var replyReviewComment = new ReviewCommentModel
            {
                ReplyCommentId = id,
                CreatedBy = User.FindFirstValue("name"),
                Title = req.Title,
                Content = req.Content,
                ProductId = req.ProductId,
                UserId = user!.Id,
                CreatedTimeUTC = DateTime.UtcNow

            };
            _appDbContext.Add(replyReviewComment);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new
            {
                Errors = errors
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReviewComment(Guid id, UpdateReviewCommentDTO req)
    {
        try
        {
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            var curReviewComment = await _appDbContext.ReviewComments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user!.Id);
            if (curReviewComment == null) return NotFound();
            curReviewComment.Title = req.Title;
            curReviewComment.Content = req.Content;
            curReviewComment.Score = req.Score;
            curReviewComment.UpdateTimeUTC = DateTime.UtcNow;
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReviewComment(Guid id)
    {
        try
        {
            ReviewCommentModel? curReviewComment; //ค่อยลองดู
            var userId = User.FindFirstValue("uid");
            UserModel? user = await _userManager.FindByIdAsync(userId!);
            //customer ลบได้แค่คอมเม้นตัวเอง
            if (User.IsInRole("Customer")) curReviewComment = await _appDbContext.ReviewComments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user!.Id);
            //Sale Admin ลบได้ทุกคอมเม้น
            else curReviewComment = await _appDbContext.ReviewComments.FirstOrDefaultAsync(c => c.Id == id);
            if (curReviewComment == null) return NotFound();
            //ดึงฟิลที่ replyReviewCommentId = reviewCommentId ที่ส่งมาทั้งหมด (แสดงว่านั่นคือ คอมเม้นตอบกลับ)
            //ใช้วิธีนี้เพราะไม่ได้ทำ relational database 
            var replyReviewComment = await _appDbContext.ReviewComments.Where(c => c.ReplyCommentId == id).ToListAsync();
            //ตรวจสอบว่ามีอย่างน้อย 1 ค่า
            if (replyReviewComment.Count != 0) _appDbContext.ReviewComments.RemoveRange(replyReviewComment); //ใช้ RemoveRange เพื่อลบหลายค่าพร้อมกัน     

            _appDbContext.ReviewComments.Remove(curReviewComment);
            await _appDbContext.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            var errors = new[] { ex.Message };
            return BadRequest(new { Errors = errors });
        }
    }



}

 */