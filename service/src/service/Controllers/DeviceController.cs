using address_service.Models;
using address_service.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using service.Models;
using SqlWorker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace address_service.Controllers
{    
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    public partial class DeviceController : ControllerBase
    {

        public DeviceController(IConfiguration configuration){
           Config = new ConfigWrap(configuration);
            _repository = Config.Repository;
        }

        public ConfigWrap Config { get; }

        private IDataRepository _repository;

        [HttpGet]
        public ActionResult<PersonObject> GetPersonByDevice(string device_id)
        {
            try
            {
                var person = _repository.GetPerson(device_id: device_id);
                if (person != null && person.HasQuarantineStop == false) return person.Person;
                return null;
            }
            catch (Exception e)
            {
                return BadRequest(ModelState);
            }
        }
        [HttpGet]
        public ActionResult<Responce> AddDevicePerson(long phone, string device_id)
        {
            try
            {
                var person = _repository.GetPerson(phone);
                if (person == null || person.HasQuarantineStop) return new Responce() { IsOk = false, Error = "Вас нет в карантине или карантин закончен для вас" };
                if(device_id == null || device_id.Length <5) return new Responce() { IsOk = false, Error = "Идентификатор вашего устройства не соответствует правилам безопасности" };
                person = _repository.GetPerson( device_id);
                if (person == null)                
                    _repository.AddDevicePerson(phone, device_id);                
                return new Responce() { IsOk = true };
            }
            catch (Exception e)
            {
                return new Responce() { IsOk = false, Error = e.ToString() };
            }
        }
        [HttpGet]
        public ActionResult<Responce> AddLocation(string device_id, double lat, double lon, int radius)
        {
            try
            {
                var person = _repository.GetPerson(device_id);
                if (person == null || person.HasQuarantineStop) return new Responce() { IsOk = false, Error = "Вас нет в карантине или карантин закончен для вас" };
                if (person.HasQuarantineStop || person.LastLocationUpdateRequest < DateTime.UtcNow.AddSeconds(-30)) return new Responce() { IsOk = true };
                if (lat > -90 && lat < 90 && lon > -180 && lon < 180)
                {
                    if (_repository.AddLocation(device_id, lat, lon, radius))
                        return new Responce() { IsOk = true };
                }
                return new Responce() { IsOk = false, Error = "Координаты неизвестны" };
            }
            catch (Exception e)
            {
                return new Responce() { IsOk = false, Error = e.Message };
            }
        }

    }
}
