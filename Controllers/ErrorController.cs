using Microsoft.AspNetCore.Mvc;

public class ErrorController : Controller
{
    [Route("Error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        switch (statusCode)
        {
                case 404:
                    ViewData["ErrorMessage"] = "عذراً، الصفحة التي تبحث عنها غير موجودة.";
            break;
                case 403:
                ViewData["ErrorMessage"] = "عذراً، ليس لديك الصلاحية للوصول إلى هذه الصفحة.";
                break;
            default:
                ViewData["ErrorMessage"] = "حدث خطأ غير متوقع في النظام.";
                break;
        }
        return View("ErrorPage");
    }
}