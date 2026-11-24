using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace OnlineCoursePlatform.Controllers
{
    [Authorize]
    public class LessonController : Controller
    {
        private readonly OnlineCoursePlatformContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        public LessonController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        { _context=context; _userManager=userManager; _environment=environment; }
        private async Task<bool> CanManageCourse(int courseId)
        {
            if (User.IsInRole("Admin")) return true;
            var user=await _userManager.GetUserAsync(User);
            return user != null && await _context.Courses.AnyAsync(c=>c.Id==courseId && c.InstructorId==user.Id);
        }
        private async Task<string?> SaveVideoAsync(IFormFile? file)
        {
            if(file==null || file.Length==0) return null;
            var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!new[]{".mp4",".webm",".ogg"}.Contains(ext) || file.Length>500L*1024*1024) throw new InvalidOperationException("الفيديو يجب أن يكون MP4 أو WEBM أو OGG وبحجم أقصى 500MB.");
            var folder=Path.Combine(_environment.WebRootPath,"uploads","videos"); Directory.CreateDirectory(folder);
            var name=Guid.NewGuid().ToString("N")+ext; using var stream=new FileStream(Path.Combine(folder,name),FileMode.Create); await file.CopyToAsync(stream); return "/uploads/videos/"+name;
        }

        // GET: Lesson/Details/5 (عرض تفاصيل ومشاهدة الدرس)
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            // أول درس (الأقدم حسب الترتيب ثم المعرّف) هو فيديو المقدمة المجاني.
            // جميع الدروس التالية محمية بالاشتراك مهما كانت قيمة IsFreePreview القديمة.
            var introductionLessonId = await _context.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.DisplayOrder).ThenBy(l => l.Id)
                .Select(l => (int?)l.Id).FirstOrDefaultAsync();
            bool isIntroduction = introductionLessonId == lesson.Id;
            bool isAuthorized = isIntroduction || User.IsInRole("Admin");
            var currentUser = await _userManager.GetUserAsync(User);

            if (!isAuthorized && currentUser != null && User.IsInRole("Instructor"))
                isAuthorized = lesson.Course.InstructorId == currentUser.Id;

            if (!isAuthorized && currentUser != null)
                isAuthorized = await _context.Enrollments.AnyAsync(e => e.StudentId == currentUser.Id && e.CourseId == lesson.CourseId);

            if (!isAuthorized)
            {
                TempData["Error"] = "انتهت معاينة المقدمة. اشترك في الدورة لمتابعة بقية الدروس.";
                return RedirectToAction("Details", "Course", new { id = lesson.CourseId });
            }
            ViewBag.IsIntroduction = isIntroduction;

            return View(lesson);
        }

        // GET: Lessons/Create?courseId=5
        public async Task<IActionResult> Create(int courseId)
        {
            var course = _context.Courses.Find(courseId);
            if (course == null)
            {
                return NotFound();
            }

            if (!await CanManageCourse(courseId)) return Forbid();
            ViewBag.CourseTitle = course.Title;
            var lesson = new Lesson { CourseId = courseId };
            return View(lesson);
        }

        // POST: Lessons/Create
        [HttpPost]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,Title,Description,VideoUrl,DisplayOrder,DurationInMinutes,IsFreePreview")] Lesson lesson, IFormFile? videoFile)
        {
            if (!await CanManageCourse(lesson.CourseId)) return Forbid();
            try { var uploaded=await SaveVideoAsync(videoFile); if(uploaded!=null) lesson.VideoUrl=uploaded; } catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); ViewBag.CourseTitle=(await _context.Courses.FindAsync(lesson.CourseId))?.Title; return View(lesson); }
            lesson.CreatedAt = DateTime.UtcNow;
            lesson.DisplayOrder = (await _context.Lessons.Where(l => l.CourseId == lesson.CourseId).Select(l => (int?)l.DisplayOrder).MaxAsync() ?? 0) + 1;
            lesson.IsFreePreview = !await _context.Lessons.AnyAsync(l => l.CourseId == lesson.CourseId);

            _context.Add(lesson);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Course", new { id = lesson.CourseId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int lessonId)
        {
            var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == lesson.CourseId);
            if (enrollment == null && !User.IsInRole("Admin") && !(User.IsInRole("Instructor") && lesson.Course.InstructorId == user.Id)) return Forbid();

            var key = $"completed-lessons:{user.Id}:{lesson.CourseId}";
            var completed = System.Text.Json.JsonSerializer.Deserialize<List<int>>(HttpContext.Session.GetString(key) ?? "[]") ?? new List<int>();
            var isFirstCompletionInSession = !completed.Contains(lesson.Id);
            if (isFirstCompletionInSession) completed.Add(lesson.Id);
            HttpContext.Session.SetString(key, System.Text.Json.JsonSerializer.Serialize(completed));
            var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == lesson.CourseId);
            if (enrollment != null && isFirstCompletionInSession && totalLessons > 0)
            {
                // تخزين النسبة في قاعدة البيانات حتى تبقى بعد انتهاء الجلسة.
                enrollment.ProgressPercentage = Math.Min(100m, enrollment.ProgressPercentage + Math.Round(100m / totalLessons, 2));
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "تم تسجيل إكمال الدرس وتحديث نسبة الإنجاز.";
            return RedirectToAction(nameof(Details), new { id = lesson.Id });
        }

        // GET: Lessons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await CanManageCourse(lesson.CourseId)) return Forbid();
            return View(lesson);
        }

        // POST: Lessons/Edit/5
        [HttpPost]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,Title,Description,VideoUrl,DisplayOrder,DurationInMinutes,IsFreePreview,CreatedAt")] Lesson lesson, IFormFile? videoFile)
        {
            if (id != lesson.Id) return NotFound();
            var existing=await _context.Lessons.FindAsync(id); if(existing==null) return NotFound();
            if (!await CanManageCourse(existing.CourseId)) return Forbid();
            try
            {
                var uploaded=await SaveVideoAsync(videoFile);
                existing.Title=lesson.Title; existing.Description=lesson.Description; existing.DisplayOrder=lesson.DisplayOrder; existing.DurationInMinutes=lesson.DurationInMinutes; existing.IsFreePreview=lesson.IsFreePreview;
                if(uploaded!=null) existing.VideoUrl=uploaded; else if(!string.IsNullOrWhiteSpace(lesson.VideoUrl)) existing.VideoUrl=lesson.VideoUrl;
                _context.Update(existing);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LessonExists(lesson.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Course", new { id = lesson.CourseId });
        }

        // POST: Lessons/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
            {
                int courseId = lesson.CourseId;
                if (!await CanManageCourse(courseId)) return Forbid();
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            return RedirectToAction("Index", "Course");
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.Id == id);
        }
    }
}