﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace address_service.Models
{
    public class PersonCacheObject
    {
        public PersonObject Person { get; set; }
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
    }    
   
    public class Location
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Radius { get; set; }
    }
    public class Responce
    {
        public bool IsOk { get; set; }
        public string Error { get; set; }
    }   
}
