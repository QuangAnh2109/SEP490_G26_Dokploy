using Microsoft.AspNetCore.Mvc;

namespace YourProject.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}