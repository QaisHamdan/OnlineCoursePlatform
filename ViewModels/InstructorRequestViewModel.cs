using System.ComponentModel.DataAnnotations;

namespace OnlineCoursePlatform.ViewModels
{
    public class InstructorRequestViewModel
    {
        [Required(ErrorMessage = "يرجى كتابة نبذة تعريفية عنك وعن خبرتك.")]
        [Display(Name = "النبذة التعريفية")]
        public string Bio { get; set; }
    }
}