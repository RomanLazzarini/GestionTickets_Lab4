using System.Diagnostics;
using GestionTickets_Lab4.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionTickets_Lab4.Controllers
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
            // SI EL USUARIO NO ESTÁ LOGUEADO -> AL LOGIN DIRECTAMENTE
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/Identity/Account/Login");
            }

            // SI SÍ ESTÁ LOGUEADO -> MUESTRA EL DASHBOARD (TU MENÚ DE BOTONES)
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
