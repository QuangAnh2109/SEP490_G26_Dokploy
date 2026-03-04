using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class QuestionController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View("QuestionList");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("QuestionAdd");
        }
    }
}
