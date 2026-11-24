using System.ComponentModel.DataAnnotations;

namespace OnlineCoursePlatform.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string Email { get; set; } = null!;
    }
}