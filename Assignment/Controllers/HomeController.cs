using Assignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Assignment.Controllers
{
    public class HomeController : Controller
    {
        // Trang chủ "/" điều hướng người dùng tới màn hình chính theo vai trò
        [Authorize]
        public IActionResult Index()
        {
            if (User.IsInRole("SinhVien"))
            {
                return RedirectToAction("MyLearning", "StudentPortal");
            }

            return RedirectToAction("Index", "Course");
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
