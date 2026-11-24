using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;

namespace OnlineCoursePlatform.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly OnlineCoursePlatformContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // POST: Enrollment/Enroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // التحقق من وجود الدورة
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            // التحقق من عدم تكرار التسجيل مسبقاً (منع التكرار)
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == courseId);

            if (existingEnrollment != null)
            {
                TempData["Message"] = "أنت مسجل في هذه الدورة مسبقاً!";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            // إنشاء سجل تسجيل جديد
            var enrollment = new Enrollment
            {
                StudentId = user.Id,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = 0,
                Status = "Active"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم التسجيل في الدورة بنجاح!";
            return RedirectToAction("Details", "Course", new { id = courseId });
        }
        // GET: Enrollment/MyCourses
        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var myEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .Where(e => e.StudentId == user.Id)
                .ToListAsync();

            return View(myEnrollments);
        }
    }
}
