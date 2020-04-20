using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication.Models;
using WebApplication.Extensions;
using qurantine.service;

namespace WebApplication.Controllers
{
    public class HomeController : Controller
    {
        private const string SessionKeyName = "Identity";
        private readonly ILogger<HomeController> _logger;
        private static Responce _lastResponceInfo;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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
                    AddQuarantine = error?.ErrorCode == ErrorCode._1
                });
#endif
            }
            return RedirectToAction( "QuarantinePersonInfo");
        }
        [HttpPost]
        public async Task<IActionResult> Registry(RegistryModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.Get<string>(SessionKeyName)))
            {
                try
                {
                    using (var client = new qurantine.service.QurantineClient())
                    {
                        if (model.DeviceId == null || model.DeviceId == string.Empty)
                            model.DeviceId = Guid.NewGuid().ToString("D");
                        var res = await client.AddDevicePersonAsync(long.Parse(model.Phone), model.DeviceId, model.AddQuarantine);
                        if ( res.IsOk)
                        {
                            HttpContext.Session.Set(SessionKeyName, model.DeviceId);
                        }
                        else
                        {
                            _lastResponceInfo = res ?? new Responce();
                            if (_lastResponceInfo.Error == null)
                                _lastResponceInfo.Error = "формат телефона не поддерживается или нет связи";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
                catch (Exception e)
                {
                    _lastResponceInfo = new Responce() { Error = e.Message };
                    return RedirectToAction("Index");
                }

            }
            return RedirectToAction("QuarantinePersonInfo");
        }

        public async Task<ActionResult> PersonsZona()
        {
            using (var client = new qurantine.service.QurantineClient())
            {
                var res = await client.GetZonaLocationsAsync();
                return View(new LocationsModel()
                {
                    Locations = res.ToArray()
                });

            }
        }
        public async Task<ActionResult> PersonsLastLocation()
        {
            using (var client = new qurantine.service.QurantineClient())
            {
                var res = await client.GetPersonsLastLocationsAsync();
                return View(new LocationsModel()
                {
                    Locations = res.ToArray()
                });

            }
        }
        public async Task<ActionResult> PersonLastLocation()
        {
            using (var client = new qurantine.service.QurantineClient())
            {
                
                var device_id = HttpContext.Session.Get<string>(SessionKeyName);
                if (device_id?.Length > 1)
                {
                    var res = await client.GetPersonLocationsAsync(null, device_id);
                    return View(new LocationsModel()
                    {
                        Locations = res.ToArray()
                    });
                }
                else
                {
                   return  BadRequest();
                }

            }
        }
        [HttpGet]
        public async Task<IActionResult> AddLocation(double lat, double lon, double radius)
        {
            var deviceId = HttpContext.Session.Get<string>(SessionKeyName);
            if (!string.IsNullOrEmpty(deviceId))
            {
                try
                {
                    using (var client = new qurantine.service.QurantineClient())
                    {
                        var res = await client.AddLocationAsync(deviceId, lat, lon, (int)radius);
                        if (res == null || res.IsOk)
                        {

                            _lastResponceInfo = null;
                            return Ok();
                        }
                        else
                        {
                            _lastResponceInfo = res;
                            return RedirectToAction("QuarantinePersonInfo");
                        }

                    }
                }
                catch (Exception e)
                {
                    _lastResponceInfo = new Responce() { Error = e.Message };
                    return RedirectToAction("QuarantinePersonInfo");
                }

            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> QuarantinePersonInfo()
        {
            try
            {
                var deviceId = HttpContext.Session.Get<string>(SessionKeyName);
                if (deviceId == null || deviceId.Length == 0) return Redirect("Index");
                using (var client = new qurantine.service.QurantineClient())
                {

                    var res = await client.GetPersonByDeviceAsync(HttpContext.Session.Get<string>(SessionKeyName));
                    return View(new QuarantineModel()
                    {
                        Person = new PersonModel(res),
                        DeviceId = deviceId,

                    });
                }
            }catch(Exception ex)
            {
                _lastResponceInfo = new Responce()
                {
                    Error = ex.Message
                };
                return RedirectToAction("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult<PersonObject>> CheckDeviceId(string deviceId )
        {
            try
            {         
                using (var client = new qurantine.service.QurantineClient())
                {
                    var res= await client.GetPersonByDeviceAsync(deviceId);
                    if (res != null)
                    {
                        HttpContext.Session.Set(SessionKeyName, deviceId);
                        return Ok(res);
                    }
                }
            }
            catch (Exception ex)
            {
             
            }
            return BadRequest();
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
