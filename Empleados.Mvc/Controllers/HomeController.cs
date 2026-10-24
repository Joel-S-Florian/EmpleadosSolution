using Empleados.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Empleados.Mvc.Controllers
{
    public class HomeController : Controller
    {
        // Sin sesión: la página pública; con sesión: el inicio del sistema
        public IActionResult Index()
        {
            return User.Identity?.IsAuthenticated == true ? View() : View("Landing");
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
