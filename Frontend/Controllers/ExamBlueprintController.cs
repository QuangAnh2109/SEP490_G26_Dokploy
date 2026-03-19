using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class ExamBlueprintController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Edit(int id)
        {
            ViewBag.EditId = id;
            return View("Create");
        }
    }
}
