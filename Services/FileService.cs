using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OnlineCoursePlatform.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        // تحديد الامتداد المسموح بها والحد الأقصى للحجم (مثلاً 5 ميجابايت)
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName = "uploads")
        {
            if (file == null || file.Length == 0)
                return null;

            // 1. التحقق من حجم الملف
            if (file.Length > MaxFileSize)
            {
                throw new InvalidOperationException("حجم الملف يتجاوز الحد المسموح به (5 ميجابايت كحد أقصى).");
            }

            // 2. التحقق من امتداد الملف
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            bool isValidExtension = false;
            foreach (var ext in _allowedExtensions)
            {
                if (extension == ext)
                {
                    isValidExtension = true;
                    break;
                }
            }

            if (!isValidExtension)
            {
                throw new InvalidOperationException("نوع الملف غير مسموح به. يجوز رفع صور فقط (jpg, jpeg, png, webp).");
            }

            // تحديد مسار مجلد الحفظ داخل wwwroot
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // توليد اسم فريد للملف باستخدام GUID
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // حفظ الملف في السيرفر
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/" + folderName + "/" + uniqueFileName;
        }

        public bool DeleteFile(string fileRelativePath)
        {
            if (string.IsNullOrEmpty(fileRelativePath))
                return false;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, fileRelativePath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
    }
}