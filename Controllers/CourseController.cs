using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;
using OnlineCoursePlatform.Services;

namespace OnlineCoursePlatform.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly OnlineCoursePlatformContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileService _fileService;

        public CourseController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager, FileService fileService)
        {
            _context = context;
            _userManager = userManager;
            _fileService = fileService;
        }

        // 1. استعراض جميع الدورات مع البحث والفلاتر (Index)
        [AllowAnonymous] // مسموح للجميع مشاهدة الدورات
        public async Task<IActionResult> Index(string searchString, int? categoryId, string priceFilter)
        {
            var coursesQuery = _context.Courses
                 .Include(c => c.Category)
                 .Include(c => c.Instructor)
                 .AsQueryable();

            // إذا لم يكن المستخدم مشرفاً (Admin)، اعرض الدورات المعتمدة فقط
            if (!User.IsInRole("Admin"))
            {
                coursesQuery = coursesQuery.Where(c => c.Status == "Approved");
            }

            // 1. تصفية حسب البحث (عنوان الدورة، وصفها، أو اسم المدرب)
            if (!string.IsNullOrEmpty(searchString))
            {
                coursesQuery = coursesQuery.Where(c =>
                    c.Title.Contains(searchString) ||
                    c.Description.Contains(searchString) ||
                    c.Instructor.UserName.Contains(searchString));
            }

            // 2. تصفية حسب الفئة (Category)
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                coursesQuery = coursesQuery.Where(c => c.CategoryId == categoryId.Value);
            }

            // 3. تصفية حسب السعر (مجاني أو مدفوع)
            if (!string.IsNullOrEmpty(priceFilter))
            {
                if (priceFilter == "free")
                {
                    coursesQuery = coursesQuery.Where(c => c.Price == 0);
                }
                else if (priceFilter == "paid")
                {
                    coursesQuery = coursesQuery.Where(c => c.Price > 0);
                }
            }

            // تمرير قيم البحث للواجهة لتبقى محفوظة في حقول الإدخال
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentPrice"] = priceFilter;

            // جلب قائمة التصنيفات لعرضها في القائمة المنسدلة (Dropdown)
            ViewBag.Categories = await _context.Categories.ToListAsync();

            var courses = await coursesQuery.ToListAsync();
            return View(courses);
        }

        // 2. عرض نموذج إنشاء دورة جديدة (GET: Create)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            if (!User.IsInRole("Instructor") && !User.IsInRole("Admin")) return Forbid();
            // جلب التصنيفات المفعلة من قاعدة البيانات
            var categories = await _context.Categories
                                           .Where(c => c.IsActive) // التأكد أنها نشطة
                                           .ToListAsync();

            // ربط القائمة المنسدلة بـ Id و Name
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name");

            return View();
        }

        // 3. استقبال البيانات وحفظ الدورة (POST: Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Price,Level,CategoryId,Status")] Course course, IFormFile? coverImage, bool isFreeCourse = true)
        {
            if (!User.IsInRole("Instructor") && !User.IsInRole("Admin")) return Forbid();
            // استبعاد التحقق عن الحقول التي سيتم تعبئتها برمجياً لمنع مشاكل الـ ModelState
            ModelState.Remove("ThumbnailUrl");
            ModelState.Remove("InstructorId");
            ModelState.Remove("Instructor");
            ModelState.Remove("Category");
            ModelState.Remove("Enrollments");
            ModelState.Remove("Lessons");

            if (isFreeCourse)
            {
                course.Price = 0m;
            }
            else if (course.Price <= 0m)
            {
                ModelState.AddModelError(nameof(course.Price), "للدورة المدفوعة أدخل سعراً أكبر من صفر.");
            }

            if (ModelState.IsValid)
            {
                // 1. رفع صورة الغلاف مع التحقق
                if (coverImage != null && coverImage.Length > 0)
                {
                    try
                    {
                        course.ThumbnailUrl = await _fileService.SaveFileAsync(coverImage, "uploads/courses");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "حدث خطأ أثناء رفع الصورة: " + ex.Message);
                        ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", course.CategoryId);
                        return View(course);
                    }
                }
                else
                {
                    course.ThumbnailUrl = null; // لا توجد صورة غلاف
                }

                // 2. جلب معرف المستخدم الحالي (المدرب)
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    course.InstructorId = user.Id;
                }
                else
                {
                    var defaultUser = _context.Users.FirstOrDefault();
                    if (defaultUser != null)
                    {
                        course.InstructorId = defaultUser.Id;
                    }
                }

                // 3. تعيين تاريخ الإنشاء وحالة الدورة لتكون معلقة بانتظار موافقة الأدمن
                course.CreatedAt = DateTime.UtcNow;
                course.Status = "Pending"; // فرض الحالة المعلقة لضمان عدم ظهورها مباشرة للعامة

                // 4. الحفظ في القاعدة
                _context.Add(course);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إرسال الدورة بنجاح وهي بانتظار موافقة الإدارة للنشر!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", course.CategoryId);
            return View(course);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Include(c => c.Lessons)
                .Include(c => c.Reviews).ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            // التحقق هل المستخدم الحالي مسجل في هذه الدورة أم لا؟
            bool isEnrolled = false;
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    isEnrolled = await _context.Enrollments
                        .AnyAsync(e => e.StudentId == user.Id && e.CourseId == id);
                }
            }

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.ProgressPercentage = 0m;
            if (isEnrolled)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.StudentId == currentUser!.Id && e.CourseId == id);
                ViewBag.ProgressPercentage = enrollment?.ProgressPercentage ?? 0m;
            }
            return View(course);
        }

        // GET : Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (!User.IsInRole("Admin") && course.InstructorId != _userManager.GetUserId(User)) return Forbid();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", course.CategoryId);
            return View(course);
        }

        // POST : Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Price,Level,CategoryId,InstructorId,CreatedAt,Status,ThumbnailUrl")] Course course, IFormFile? coverImage, bool isFreeCourse = false)
        {
            if (id != course.Id) return NotFound();
            var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingCourse == null) return NotFound();
            if (!User.IsInRole("Admin") && existingCourse.InstructorId != _userManager.GetUserId(User)) return Forbid();

            ModelState.Remove("ThumbnailUrl");
            ModelState.Remove("Instructor");
            ModelState.Remove("Category");

            if (isFreeCourse)
            {
                course.Price = 0m;
            }
            else if (course.Price <= 0m)
            {
                ModelState.AddModelError(nameof(course.Price), "للدورة المدفوعة أدخل سعراً أكبر من صفر.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. معالجة رفع صورة جديدة إن وجدت، وإلا الاحتفاظ بالصورة القديمة
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        string newThumbnail = await _fileService.SaveFileAsync(coverImage, "uploads/courses");
                        course.ThumbnailUrl = newThumbnail;
                    }

                    existingCourse.Title=course.Title; existingCourse.Description=course.Description; existingCourse.Price=course.Price; existingCourse.Level=course.Level; existingCourse.CategoryId=course.CategoryId; existingCourse.UpdatedAt=DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(course.ThumbnailUrl)) existingCourse.ThumbnailUrl=course.ThumbnailUrl;
                    if (coverImage != null && coverImage.Length > 0) existingCourse.ThumbnailUrl=course.ThumbnailUrl;
                    _context.Update(existingCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return User.IsInRole("Instructor") && !User.IsInRole("Admin")
                    ? RedirectToAction("Dashboard", "Instructor")
                    : RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", course.CategoryId);
            return View(course);
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // GET : Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();
            if (!User.IsInRole("Admin") && course.InstructorId != _userManager.GetUserId(User)) return Forbid();

            return View(course);
        }

        // POST : Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            if (!User.IsInRole("Admin") && course.InstructorId != _userManager.GetUserId(User)) return Forbid();

            // التحقق مما إذا كان هناك مدفوعات أو تسجيلات مرتبطة بهذه الدورة
            bool hasPayments = _context.Payments.Any(p => p.CourseId == id);
            bool hasEnrollments = _context.Enrollments.Any(e => e.CourseId == id);

            if (hasPayments || hasEnrollments)
            {
                ModelState.AddModelError(string.Empty, "عذراً، لا يمكن حذف هذه الدورة لوجود طلاب مسجلين أو معاملات مالية مرتبطة بها.");
                return View(course);
            }

            // الحذف بنجاح إذا لم تكن مرتبطة بسجلات
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return User.IsInRole("Instructor") && !User.IsInRole("Admin")
                ? RedirectToAction("Dashboard", "Instructor")
                : RedirectToAction(nameof(Index));
        }

        // --- دوال لوحة تحكم الأدمن للموافقة على الدورات ---

        // عرض الدورات التي تنتظر موافقة الأدمن
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingCourses()
        {
            var pendingCourses = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .Where(c => c.Status == "Pending")
                .ToListAsync();

            return View(pendingCourses);
        }

        // زر الموافقة على الدورة ونشرها
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.Status = "Approved"; // تغيير الحالة لتصبح معتمدة وتظهر للجميع
            _context.Update(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت الموافقة على الدورة ونشرها بنجاح!";
            return RedirectToAction(nameof(PendingCourses));
        }
    }
}