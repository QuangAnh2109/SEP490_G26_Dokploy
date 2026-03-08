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

        [HttpGet("Question/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            ViewBag.QuestionId = id;
            return View("QuestionEdit"); 
        }
    }
}
