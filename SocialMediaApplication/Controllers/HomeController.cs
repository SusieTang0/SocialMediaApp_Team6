using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SocialMediaApplication.Models;
using System.Diagnostics;
using System.Linq;


namespace SocialMediaApplication.Controllers
{
    public class HomeController : Controller
    {


        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return Login();
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

        public IActionResult SignUp()
        {
            return View("./Views/Account/Register.cshtml");
        }

        public IActionResult Login()
        {
            return View("./Views/Account/Login.cshtml");
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Sign out the user from the authentication system
            await HttpContext.SignOutAsync();

            // Redirect to the Home/Index page
            return RedirectToAction("Index", "Home");
        }
    }
}
