using Microsoft.AspNetCore.Mvc;

namespace AITourismPlanner.Controllers
{
    public class HotelsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {

            ViewBag.Id = id;
            return View();
        }
    }
}