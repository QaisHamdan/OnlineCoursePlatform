using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace OnlineCoursePlatform.ViewModels
{
    public class UserProfileViewModel
    {
        [Required, StringLength(100)] public string FirstName { get; set; } = null!;
        [Required, StringLength(100)] public string LastName { get; set; } = null!;
        [Required, EmailAddress] public string Email { get; set; } = null!;
        [Phone] public string? PhoneNumber { get; set; }
        [StringLength(1000)] public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string? InstructorStatus { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
