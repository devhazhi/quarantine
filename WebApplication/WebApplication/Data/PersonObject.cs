using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace service.Models
{
    public class PersonCacheObject
    {
        public PersonObject Person { get; set; }
        public Location LastLocation { get; set; }
        public DateTime? LastLocationUpdateRequest { get; set; }
        public DateTime? LastUpdate { get; set; }
        public bool HasQuarantineStop => Person.QuarantineStopUnix < (long)DateTime.UtcNow.Subtract(UnixStart).TotalSeconds;
        public readonly static DateTime UnixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    }
    public class PersonObject
    {
        public Location Zone { get; set; }
        public string Name { get; set; }
        public long QuarantineStopUnix { get; set; }
        public string Token { get; set; }

    }    
   
    public class Location
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Radius { get; set; }
    }

    public class LocationWithTime : Location
    {
        public long? UnixUtcTime { get; set; }
    }
    public class Responce
    {
        public bool IsOk { get; set; }
        public string Error { get; set; }
        public ErrorCode ErrorCode { get; set; }
    }
    public class DeviceFileInfo
    {
        public string DeviceId { get; set; }
        public byte[] Data { get; set; }        
        public DeviceFileTypeEnum FileType { get; set; }
    }
    public class DeviceLocationInfo
    {
        public string DeviceId { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Radius { get; set; }
    }
    public class AttachDeviceInfo
    {
        public string Phone { get; set; }
        public string DeviceId { get; set; }
        public bool AddQuarantine { get; set; }
    }
    public enum DeviceFileTypeEnum : int { Jpeg = 1 };
    public enum ErrorCode : int { NotError = 0, NotQuarantine, NotDevice, FormatDeviceIdNotSupport, FormatTokenNotSupport, CoordinateFailed, AccessDeny, NotSupport };

}
