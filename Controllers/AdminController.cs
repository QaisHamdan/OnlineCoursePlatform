using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;

namespace OnlineCoursePlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly OnlineCoursePlatformContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(OnlineCoursePlatformContext context,
                               UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Index (لوحة الإحصائيات العامة)
        public async Task<IActionResult> Index()
        {
            ViewBag.UsersCount = await _context.Users.CountAsync();
            ViewBag.CoursesCount = await _context.Courses.CountAsync();
            ViewBag.EnrollmentsCount = await _context.Enrollments.CountAsync();
            ViewBag.CategoriesCount = await _context.Categories.CountAsync();
            ViewBag.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
            ViewBag.PendingInstructorsCount = await _context.Users.CountAsync(u => u.InstructorStatus == "Pending");
            ViewBag.PendingCoursesCount = await _context.Courses.CountAsync(c => c.Status == "Pending");

            return View();
        }

        // GET: Admin/Dashboard (إدارة طلبات انضمام المدربين المعلقة)
        public async Task<IActionResult> Dashboard()
        {
            var pendingInstructors = await _context.Users
                .Where(u => u.InstructorStatus == "Pending")
                .ToListAsync();

            return View(pendingInstructors);
        }

        // POST: Admin/ApproveInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveInstructor(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.InstructorStatus = "Approved";
            await _userManager.UpdateAsync(user);

            if (!await _roleManager.RoleExistsAsync("Instructor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Instructor"));
            }
            await _userManager.AddToRoleAsync(user, "Instructor");

            TempData["Success"] = $"تمت الموافقة على الطلب وأصبح المستخدم ({user.Email}) مدرباً معتمداً.";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/RejectInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectInstructor(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.InstructorStatus = "Rejected";
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"تم رفض طلب المدرب للمستخدم ({user.Email}).";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Admin/Users (إدارة جميع المستخدمين/الطلاب)
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                bool hasRelatedData = await _context.Courses.AnyAsync(c => c.InstructorId == id)
                    || await _context.Enrollments.AnyAsync(e => e.StudentId == id)
                    || await _context.Payments.AnyAsync(p => p.StudentId == id)
                    || await _context.Reviews.AnyAsync(r => r.StudentId == id);

                if (hasRelatedData)
                {
                    TempData["Error"] = "لا يمكن حذف هذا المستخدم لوجود دورات أو تسجيلات أو مدفوعات أو تقييمات مرتبطة بحسابه. يمكنك إيقاف الحساب بدلاً من حذفه.";
                    return RedirectToAction(nameof(Users));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المستخدم بنجاح.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Admin/Courses (إدارة ومراجعة دورات المنصة)
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .ToListAsync();

            return View(courses);
        }

        // POST: Admin/ApproveCourse (الموافقة على الدورة لتصبح معتمدة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.Status = "Approved";
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت الموافقة على الدورة بنجاح وأصبحت متاحة للعامة.";
            return RedirectToAction(nameof(Courses));
        }

        // POST: Admin/RejectCourse (رفض الدورة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفض الدورة.";
            return RedirectToAction(nameof(Courses));
        }

        // GET: Admin/Comments (إدارة ومراجعة التعليقات والتقييمات)
        public async Task<IActionResult> Comments()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Course)
                .Include(r => r.Student)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        // POST: Admin/DeleteComment (حذف تعليق غير لائق أو مسيء)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف التعليق بنجاح.";
            }
            return RedirectToAction(nameof(Comments));
        }

        // GET: Admin/Payments (عرض تفاصيل وسجلات المدفوعات المالية)
        public async Task<IActionResult> Payments(string? status, string? provider, DateTime? fromDate, DateTime? toDate, string? search)
        {
            var query = _context.Payments.Include(p => p.Course).Include(p => p.Student).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.PaymentStatus == status);
            if (!string.IsNullOrWhiteSpace(provider)) query = query.Where(p => p.PaymentProvider == provider);
            if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(p => p.CreatedAt < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.TransactionId.Contains(search) || p.Student.Email!.Contains(search) || p.Course.Title.Contains(search));
            ViewBag.Status=status; ViewBag.Provider=provider; ViewBag.FromDate=fromDate?.ToString("yyyy-MM-dd"); ViewBag.ToDate=toDate?.ToString("yyyy-MM-dd"); ViewBag.Search=search;
            ViewBag.Providers=await _context.Payments.Select(p=>p.PaymentProvider).Distinct().ToListAsync();
            ViewBag.Statuses=await _context.Payments.Select(p=>p.PaymentStatus).Distinct().ToListAsync();
            return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        // GET: Admin/AdminsManagement (إدارة حسابات الأدمن والصلاحيات)
        public async Task<IActionResult> AdminsManagement()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            return View(admins);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string email, string firstName, string lastName, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(password) || password.Length < 6)
            { TempData["Error"]="أدخل البريد والاسم وكلمة مرور لا تقل عن 6 أحرف."; return RedirectToAction(nameof(AdminsManagement)); }
            if (await _userManager.FindByEmailAsync(email) != null) { TempData["Error"]="هذا البريد مسجل مسبقاً."; return RedirectToAction(nameof(AdminsManagement)); }
            var user=new ApplicationUser { UserName=email, Email=email, FirstName=firstName.Trim(), LastName=(lastName ?? "").Trim(), EmailConfirmed=true, IsActive=true, CreatedAt=DateTime.UtcNow };
            var created=await _userManager.CreateAsync(user,password);
            if (!created.Succeeded) { TempData["Error"]=string.Join(" | ",created.Errors.Select(e=>e.Description)); return RedirectToAction(nameof(AdminsManagement)); }
            await _userManager.AddToRoleAsync(user,"Admin"); TempData["Success"]="تم إنشاء حساب الأدمن بنجاح."; return RedirectToAction(nameof(AdminsManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user=await _userManager.FindByIdAsync(userId); if(user==null) return NotFound();
            if (await _userManager.IsInRoleAsync(user,"Admin")) { TempData["Error"]="لا يمكن إيقاف حساب أدمن من هذه الصفحة."; return RedirectToAction(nameof(Comments)); }
            user.IsActive=false; await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Success"]="تم إيقاف حساب المستخدم. لن يتمكن من تسجيل الدخول."; return RedirectToAction(nameof(Comments));
        }
    }
}