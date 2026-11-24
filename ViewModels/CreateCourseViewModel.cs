using System.ComponentModel.DataAnnotations;

namespace OnlineCoursePlatform.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "عنوان الدورة مطلوب")]
        [StringLength(100, ErrorMessage = "العنوان يجب ألا يتجاوز 100 حرف")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "وصف الدورة مطلوب")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0, 10000, ErrorMessage = "السعر يجب أن يكون بين 0 و 10000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "مستوى الدورة مطلوب")]
        public string Level { get; set; } = null!; // مبتدئ، متوسط، متقدم

        [Required(ErrorMessage = "التصنيف مطلوب")]
        public int CategoryId { get; set; }

        public string? ThumbnailUrl { get; set; }
    }
}