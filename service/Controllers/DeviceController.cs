using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using service.Models;
using service.Utils;
using SqlWorker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
namespace service.Controllers
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
        public async Task<ActionResult<PersonObject>> GetPersonByDevice(string device_id)
        {
            try
            {
                var person = await _repository.GetPerson(device_id: device_id);
                if (person != null && person.HasQuarantineStop == false) return Ok(person.Person);
                return null;
            }
            catch (Exception e)
            {
                return BadRequest(ModelState);
            }
        }
        [HttpGet]
        public async Task<ActionResult<Responce>> AddDevicePerson(long phone, string device_id)
        {
            try
            {
                var person = await _repository.GetPerson(phone);
                if (!person.Check()) return ErrorCode.NotQuarantine.getResponce();
                if (!device_id.CheckFormatDeviceId()) return ErrorCode.FormatDeviceIdNotSupport.getResponce();
                person = await _repository.GetPerson( device_id);
                if (person == null)                
                    _repository.AddDevicePerson(phone, device_id);                
                return new Responce() { IsOk = true };
            }
            catch (Exception e)
            {
                return e.getResponce();
            }
        }

        [HttpGet]
        public async Task<ActionResult<Responce>> AddDeviceNotificationToken(string device_id, string token)
        {
            try
            {
                if (token == null || token.Length < 15)
                    return ErrorCode.FormatTokenNotSupport.getResponce();
                if (!device_id.CheckFormatDeviceId()) return ErrorCode.FormatDeviceIdNotSupport.getResponce();
                var person = await _repository.GetPerson(device_id);
                if (!person.Check()) return ErrorCode.NotQuarantine.getResponce();
                _repository.AddDeviceNotificationToken(device_id, token);
                return new Responce() { IsOk = true };
            }
            catch (Exception e)
            {
                return  e.getResponce() ;
            }
        }

        [HttpGet]
        public async Task<ActionResult<Responce>> AddLocation(string device_id, double lat, double lon, int radius)
        {
            try
            {
                var person = await _repository.GetPerson(device_id);
                if (!person.Check()) return ErrorCode.NotQuarantine.getResponce();
                if (person.LastLocationUpdateRequest < DateTime.UtcNow.AddSeconds(-30)) return new Responce() { IsOk = true };
                if (lat > -90 && lat < 90 && lon > -180 && lon < 180)
                {
                    if (_repository.AddLocation(device_id, lat, lon, radius))
                        return new Responce() { IsOk = true };
                }
                return ErrorCode.CoordinateFailed.getResponce();
            }
            catch (Exception e)
            {
                return e.getResponce();
            }
        }
        [HttpGet]
        public async Task<ActionResult<NotificationSubscribeInfoResponce>> GetSubscribeNotificationInfo(string device_id)
        {
            try
            {
                var person = await _repository.GetPerson(device_id);
                if (!person.Check()) return (NotificationSubscribeInfoResponce)ErrorCode.NotQuarantine.getResponce();

                return new NotificationSubscribeInfoResponce()
                {
                    IsOk = true,
                    Topics = _repository.GetNotifications()?.
                        Where(w => !w.EndTime.HasValue || w.EndTime.Value.Date < DateTime.UtcNow.Date).Select(a => a.Topic).Where(w=>w?.Length >0).Distinct().ToArray()
                };
            }
            catch (Exception e)
            {
                return (NotificationSubscribeInfoResponce)e.getResponce();
            }
        }
        [HttpPost]
        public async Task<ActionResult<Responce>> AddFileByDevice(DeviceFileInfo deviceFile)
        {
            try
            {
                if(deviceFile == null || deviceFile.DeviceId == null) return new Responce() { IsOk = false, Error="Нет данных об устройстве для записи" };
                if (deviceFile.Data == null || deviceFile.Data.Length == 0) return new Responce() { IsOk = false, Error = "Нет данных об устройстве для записи" };
                if (deviceFile.Data == null || deviceFile.Data.Length > Config.MaxDeviceFileSizeBytes)
                    return new Responce() { IsOk = false,
                        Error = $"Данные привышают максимальный размер ({Config.MaxDeviceFileSizeBytes / 1024.0}кб)",
                        ErrorCode = ErrorCode.NotSupport 
                    };
                string extension = "";
                switch (deviceFile.FileType)
                {
                    case DeviceFileTypeEnum.Jpeg: extension = ".jpg"; break;
                    default: return ErrorCode.NotSupport.getResponce();
                }
                var person = await _repository.GetPerson(deviceFile.DeviceId);
                if (!person.Check()) return ErrorCode.NotQuarantine.getResponce();
                _repository.AddDeviceFile(deviceFile.DeviceId, DateTime.UtcNow.ToString("yyyyMMddHHmm") + extension);
                return new Responce() { IsOk = true };
            }
            catch (Exception e)
            {
                return e.getResponce();
            }
        }
        [HttpGet]
        public ActionResult<Responce> GetStartNotification(string device_id, string topic, string title, string message)
        {
            try
            {
                if(device_id == "magomedov.gadzhi")
                {
                    NotificationManager.Instance.SendNotification(topic, title, message);
                    return new Responce() { IsOk = true };
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

    }
}
