using System.ComponentModel.DataAnnotations;

namespace OnlineCoursePlatform.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "تذكرني؟")]
        public bool RememberMe { get; set; }
    }
}