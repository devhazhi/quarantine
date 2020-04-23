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
        private const string SessionKeyName = "Identity";
        private readonly ILogger<HomeController> _logger;
        private readonly IDataRepository _repository;
        private static Responce _lastResponceInfo;

        public HomeController(ILogger<HomeController> logger, IDataRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.Get<string>(SessionKeyName)))
            {
#if DEBUG4

                HttpContext.Session.Set<string>(SessionKeyName, "c11dd068-79e1-4024-9ad9-ff6e3842cb77");
#else
            
           
                var error = _lastResponceInfo;
                _lastResponceInfo = null;
                return View(new RegistryModel()
                {
                    DeviceId = Guid.NewGuid().ToString("d"),
                    ErrorInfo = error?.Error,
                    AddQuarantine = true
                });
#endif
            }
            return RedirectToAction( "QuarantinePersonInfo");
        }
        [HttpPost]
        public IActionResult Registry(RegistryModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.Get<string>(SessionKeyName)))
            {
                return RedirectToAction("Index");
            }
            return RedirectToAction("QuarantinePersonInfo");
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

        public async Task<IActionResult> QuarantinePersonInfo()
        {
            try
            {
                var deviceId = HttpContext.Session.Get<string>(SessionKeyName);
                if (deviceId == null || deviceId.Length == 0) return Redirect("Index");

                var res = await _repository.GetPerson(HttpContext.Session.Get<string>(SessionKeyName));
                return View(new QuarantineModel()
                {
                    Person = new PersonModel(res.Person),
                    DeviceId = deviceId,

                });

            }
            catch (Exception ex)
            {
                _lastResponceInfo = new Responce()
                {
                    Error = ex.Message
                };
                return RedirectToAction("Error");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SessionKeyName);
            return RedirectToAction("Index");
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorMessage = _lastResponceInfo?.Error  });
        }
    }
}
