using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;
using OnlineCoursePlatform.ViewModels;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace OnlineCoursePlatform.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly OnlineCoursePlatformContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        public InstructorController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        { _context=context; _userManager=userManager; _environment=environment; }

        // لوحة تحكم المدرب - عرض الدورات الخاضعة لملفه
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var courses = await _context.Courses
                .Include(c => c.Category)
                .Where(c => c.InstructorId == user.Id)
                .ToListAsync();

            return View(courses);
        }

        // الخطوة 1: عرض نموذج إضافة الدورة
        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // الخطوة 1: حفظ بيانات الدورة الأساسية (Draft)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                Level = model.Level,
                CategoryId = model.CategoryId,
                InstructorId = user.Id,
                ThumbnailUrl = model.ThumbnailUrl ?? "/images/default-course.jpg",
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء مسودة الدورة بنجاح. يمكنك الآن إضافة المحتوى والدروس.";
            return RedirectToAction("ManageLessons", new { courseId = course.Id });
        }

        // الخطوة 2: صفحة إدارة الدروس والمحتوى للدورة
        [HttpGet]
        public async Task<IActionResult> ManageLessons(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var course = await _context.Courses.Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == user.Id);
            if (course == null) return NotFound();

            return View(course);
        }

        // الخطوة 2: إضافة درس جديد للدورة
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<IActionResult> AddLesson(int courseId, string title, string? videoUrl, string? description, int? durationInMinutes, IFormFile? videoFile)
        {
            var user = await _userManager.GetUserAsync(User);
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (user == null || course.InstructorId != user.Id) return Forbid();

            if (videoFile != null && videoFile.Length > 0)
            {
                var ext=Path.GetExtension(videoFile.FileName).ToLowerInvariant();
                if (!new[]{".mp4",".webm",".ogg"}.Contains(ext) || videoFile.Length > 500L*1024*1024)
                { TempData["Error"]="الفيديو يجب أن يكون MP4 أو WEBM أو OGG وبحجم أقصى 500MB."; return RedirectToAction("ManageLessons", new { courseId }); }
                var folder=Path.Combine(_environment.WebRootPath,"uploads","videos"); Directory.CreateDirectory(folder);
                var fileName=Guid.NewGuid().ToString("N")+ext; using var stream=new FileStream(Path.Combine(folder,fileName),FileMode.Create); await videoFile.CopyToAsync(stream); videoUrl="/uploads/videos/"+fileName;
            }
            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = title,
                VideoUrl = videoUrl,
                Description = description,
                DurationInMinutes = durationInMinutes,
                DisplayOrder = (await _context.Lessons.Where(l => l.CourseId == courseId).Select(l => (int?)l.DisplayOrder).MaxAsync() ?? 0) + 1,
                IsFreePreview = !await _context.Lessons.AnyAsync(l => l.CourseId == courseId),
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الدرس بنجاح!";
            return RedirectToAction("ManageLessons", new { courseId });
        }

        // الخطوة 3: إرسال الدورة للمراجعة والموافقة من قِبل الأدمن
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForApproval(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (user == null || course.InstructorId != user.Id) return Forbid();

            course.Status = "Pending";
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إرسال الدورة للمراجعة بنجاح! ستظهر للطلاب فور موافقة الأدمن عليها.";
            return RedirectToAction("Dashboard");
        }
    }
}