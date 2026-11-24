using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCoursePlatform.Models; // ??? ??? ??? Context ????? ???? ???? Models

namespace OnlineCoursePlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OnlineCoursePlatformContext _context; // ??????? ????? ?????? ??? Context

        public HomeController(ILogger<HomeController> logger, OnlineCoursePlatformContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ??? ???? ??????? ???????? ??? ??????? ?? ?????? ????????
            var courses = await _context.Courses
                .Where(c => c.Status == "Approved")
                .OrderByDescending(c => c.Id)
                .Take(3)
                .ToListAsync();

            return View(courses);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}