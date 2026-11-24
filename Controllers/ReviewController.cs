using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;

[Authorize]
public class ReviewController : Controller
{
    private readonly OnlineCoursePlatformContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int courseId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "اختر تقييماً من نجمة إلى خمس نجوم.";
            return RedirectToAction("Details", "Course", new { id = courseId });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        // التأكد هل الطالب مسجل في الدورة أولاً (اختياري لرفع مستوى الواقعية)
        var isEnrolled = await _context.Enrollments
            .AnyAsync(e => e.StudentId == user.Id && e.CourseId == courseId);

        if (!isEnrolled)
        {
            TempData["Error"] = "يجب عليك الاشتراك في الدورة أولاً لتتمكن من تقييمها!";
            return RedirectToAction("Details", "Course", new { id = courseId }); // 👈 استبدلنا Redirect بـ RedirectToAction
        }

        // التحقق إذا كان الطالب قد قيّم الدورة مسبقاً (لتعديل تقييمه أو منعه من التكرار)
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.StudentId == user.Id && r.CourseId == courseId);

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Comment = comment;
            existingReview.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var review = new Review
            {
                CourseId = courseId,
                StudentId = user.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.Add(review);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "تم إضافة تقييمك بنجاح!";
        return RedirectToAction("Details", "Course", new { id = courseId });
    }
}