using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using OnlineCoursePlatform.Models;
using OnlineCoursePlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. تسجيل الـ DbContext الخاص بقاعدة البيانات
builder.Services.AddDbContext<OnlineCoursePlatformContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. تسجيل خدمة رفع وإدارة الملفات
builder.Services.AddScoped<FileService>();

// [مضاف حديثاً] تسجيل خدمة PayPal للاتصال ببوابة الدفع
builder.Services.AddHttpClient<PayPalService>();

// 3. تسجيل خدمات الـ Identity مع تحديد قواعد كلمات المرور
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<OnlineCoursePlatformContext>()
.AddDefaultTokenProviders();

// إعدادات أمان الكوكيز لخاصية "تذكرني" والجلسات الآمنة
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // مدة الاحتفاظ بتذكر تسجيل الدخول
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var currentUser = await userManager.GetUserAsync(context.Principal!);
        if (currentUser == null || !currentUser.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

// [مهم جداً] إضافة وتفعيل الخدمات الخاصة بالـ Session لحفظ الـ courseId مؤقتاً
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(20); // مدة صلاحية الجلسة
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// تفعيل صفحة الأخطاء المخصصة لـ 404 و 403 وغيرها
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseRouting();

// [مهم جداً] تفعيل الـ Session Middleware هنا قبل المصادقة والتوجيه
app.UseSession();

// 4. تفعيل المصادقة والتحقق من الصلاحيات
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// إنشاء الأدوار وحساب الـ Admin الافتراضي تلقائياً عند بدء التشغيل
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. تعريف الأدوار الأساسية
        string[] roleNames = { "Admin", "Instructor", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. إنشاء حساب Admin افتراضي إذا لم يكن موجوداً
        var adminEmail = "admin@courseplatform.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createAdmin = await userManager.CreateAsync(user, "Admin@123");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "حدث خطأ أثناء إنشاء الأدوار أو حساب الـ Admin.");
    }
}

app.Run();