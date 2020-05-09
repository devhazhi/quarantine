using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication.Models;
using WebApplication.Extensions;
using service.Models;

namespace WebApplication.Controllers
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
            return View(new RegistryModel()
            {
                DeviceId = Guid.NewGuid().ToString("d"),
                AddQuarantine = true
            });
        }

        public ActionResult PersonsZona()
        {
            return View();
        }
        public ActionResult PersonsLastLocation()
        {
            return View();
        }
        public ActionResult PersonLastLocation()
        {
            return View();
        }

        public IActionResult QuarantinePersonInfo()
        {
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Index");
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier  });
        }
    }
}
