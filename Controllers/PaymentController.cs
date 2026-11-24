using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models;
using OnlineCoursePlatform.Services; // إضافة خدمة باي بال

public class PaymentController : Controller
{
    private readonly OnlineCoursePlatformContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PayPalService _payPalService; // حقن خدمة باي بال

    public PaymentController(OnlineCoursePlatformContext context, UserManager<ApplicationUser> userManager, PayPalService payPalService)
    {
        _context = context;
        _userManager = userManager;
        _payPalService = payPalService;
    }

    // حفظ الدورة مؤقتاً بالسيشن وتوجيه الزائر لتسجيل الدخول
    [AllowAnonymous]
    public IActionResult PrepareCheckout(int courseId)
    {
        // 1. حفظ رقم الدورة بالسيشن
        HttpContext.Session.SetInt32("PendingCourseId", courseId);

        // 2. التوجيه لصفحة Login العادية مع الـ ReturnUrl الصحيح لصفحة الـ Checkout
        string returnUrl = Url.Action("Checkout", "Payment", new { courseId = courseId });
        return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
    }

    // عرض ملخص الدفع والتحضير للتوجه إلى باي بال
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout(int courseId)
    {
        if (courseId == 0)
        {
            int? sessionCourseId = HttpContext.Session.GetInt32("PendingCourseId");
            if (sessionCourseId.HasValue)
            {
                courseId = sessionCourseId.Value;
                HttpContext.Session.Remove("PendingCourseId");
            }
        }

        if (courseId == 0)
        {
            TempData["Error"] = "الرجاء اختيار دورة أولاً لإتمام الاشتراك.";
            return RedirectToAction("Index", "Course");
        }

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        // تمرير بيانات الدورة للـ View لعرض ملخص السعر قبل الدفع
        var model = new PaymentViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            Amount = course.Price
        };

        return View("CheckoutForm", model);
    }

    // [جديد] إرسال الطلب إلى PayPal والتوجه لبوابة الدفع الخارجية
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayPalPayment(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        // الروابط التي سيعود إليها المستخدم بعد نجاح أو إلغاء الدفع من باي بال
        string returnUrl = Url.Action("ExecutePayment", "Payment", new { courseId = course.Id }, Request.Scheme);
        string cancelUrl = Url.Action("CancelPayment", "Payment", new { courseId = course.Id }, Request.Scheme);

        try
        {
            decimal amount = course.Price;
            string currency = "USD"; // عملة التعامل الافتراضية

            // استدعاء خدمة باي بال للحصول على رابط الموافقة (Approval URL)
            string approvalUrl = await _payPalService.CreateOrderAsync(amount, currency, returnUrl, cancelUrl);

            // توجيه الطالب مباشرة إلى موقع PayPal لإتمام الدفع التجريبي
            return Redirect(approvalUrl);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "حدث خطأ أثناء الاتصال ببوابة الدفع: " + ex.Message;
            return RedirectToAction("Checkout", new { courseId = courseId });
        }
    }

    // [جديد] استقبال عودة المستخدم بعد نجاح الدفع من PayPal وتأكيد العملية
    [Authorize]
    public async Task<IActionResult> ExecutePayment(int courseId, string token)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        try
        {
            // تأكيد الطلب رسميا عبر باي بال باستخدام الـ token
            bool isCompleted = await _payPalService.CaptureOrderAsync(token);

            if (isCompleted)
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return NotFound();

                // 1. تسجيل الدفع في جدول Payments
                var payment = new Payment
                {
                    StudentId = user.Id,
                    CourseId = courseId,
                    Amount = course.Price,
                    TransactionId = token, // رقم معاملة باي بال
                    PaymentProvider = "PayPal",
                    PaymentStatus = "Completed",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);

                // 2. تسجيل الطالب في الدورة (Enrollment) إذا لم يكن مسجلاً
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == courseId);

                if (existingEnrollment == null)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = user.Id,
                        CourseId = courseId,
                        EnrolledAt = DateTime.UtcNow,
                        ProgressPercentage = 0,
                        Status = "Active"
                    };
                    _context.Enrollments.Add(enrollment);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "تمت عملية الدفع بنجاح عبر PayPal وتم تسجيلك في الدورة!";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "فشل تأكيد عملية الدفع: " + ex.Message;
        }

        TempData["Error"] = "لم تتم عملية الدفع بنجاح.";
        return RedirectToAction("Checkout", new { courseId = courseId });
    }

    // [جديد] في حال قام المستخدم بإلغاء عملية الدفع من صفحة باي بال
    [Authorize]
    public IActionResult CancelPayment(int courseId)
    {
        TempData["Error"] = "تم إلغاء عملية الدفع من قبل المستخدم.";
        return RedirectToAction("Checkout", new { courseId = courseId });
    }
}