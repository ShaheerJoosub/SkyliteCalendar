using Microsoft.AspNetCore.Mvc;

namespace SkyliteCalendar.Controllers
{
    public class DisplayController : Controller
    {
        // Full-screen display board — designed to be projected on the wall
        public IActionResult Index()
        {
            return View();
        }
    }
}
