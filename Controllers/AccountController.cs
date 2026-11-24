using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineCoursePlatform.Models;
using OnlineCoursePlatform.ViewModels;

namespace OnlineCoursePlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                int? sessionCourseId = HttpContext.Session.GetInt32("PendingCourseId");
                if (sessionCourseId.HasValue)
                {
                    returnUrl = Url.Action("Checkout", "Payment", new { courseId = sessionCourseId.Value });
                }
            }

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var signedInUser = await _userManager.FindByEmailAsync(model.Email);
                    if (signedInUser == null || !signedInUser.IsActive)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "هذا الحساب موقوف حالياً. يرجى التواصل مع الإدارة.");
                        return View(model);
                    }
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // تسجيل كل مستخدم جديد كـ "طالب" (Student) افتراضياً
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Student"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    }
                    await _userManager.AddToRoleAsync(user, "Student");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // عرض وتعديل الملف الشخصي
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(new UserProfileViewModel { FirstName=user.FirstName, LastName=user.LastName, Email=user.Email!, PhoneNumber=user.PhoneNumber, Bio=user.Bio, ProfilePictureUrl=user.ProfilePictureUrl, InstructorStatus=user.InstructorStatus, Roles=await _userManager.GetRolesAsync(user) });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            ModelState.Remove(nameof(model.ProfilePictureUrl));
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(ext) || model.ProfilePicture.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(model.ProfilePicture), "الصورة يجب أن تكون JPG أو PNG أو WEBP وبحجم لا يتجاوز 5MB.");
            }
            if (!ModelState.IsValid)
            {
                model.Roles = await _userManager.GetRolesAsync(user); model.ProfilePictureUrl = user.ProfilePictureUrl; model.InstructorStatus=user.InstructorStatus; return View(model);
            }
            user.FirstName=model.FirstName.Trim(); user.LastName=model.LastName.Trim(); user.PhoneNumber=model.PhoneNumber; user.Bio=model.Bio;
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var folder=Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles"); Directory.CreateDirectory(folder);
                var fileName=Guid.NewGuid().ToString("N")+Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                using var stream=new FileStream(Path.Combine(folder,fileName),FileMode.Create); await model.ProfilePicture.CopyToAsync(stream);
                user.ProfilePictureUrl="/uploads/profiles/"+fileName;
            }
            var result=await _userManager.UpdateAsync(user);
            if (!result.Succeeded) { foreach(var error in result.Errors) ModelState.AddModelError(string.Empty,error.Description); model.Roles=await _userManager.GetRolesAsync(user); return View(model); }
            TempData["Success"]="تم تحديث الملف الشخصي بنجاح."; return RedirectToAction(nameof(Profile));
        }

        // مسار تقديم طلب الانضمام كمدرب (للطالب فقط)
        [HttpGet]
        [Authorize(Roles = "Student")]
        public IActionResult BecomeInstructor()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeInstructor(InstructorRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.InstructorStatus = "Pending";
            user.Bio = model.Bio;

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "تم إرسال طلبك بنجاح، سيقوم المشرف بمراجعته قريباً.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null) return RedirectToAction("ForgotPasswordConfirmation");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, protocol: Request.Scheme);

                TempData["ResetLink"] = callbackUrl;
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null) ModelState.AddModelError("", "رمز إعادة التعيين غير صالح.");
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "تم تغيير كلمة المرور بنجاح!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();
    }
}