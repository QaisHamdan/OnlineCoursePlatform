using System.ComponentModel.DataAnnotations;

namespace OnlineCoursePlatform.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور على الأقل {2} أحرف.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقتين.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "يرجى اختيار نوع الحساب")]
        [Display(Name = "نوع الحساب")]
        public string Role { get; set; } = "Student"; // افتراضياً طالب
    }
}