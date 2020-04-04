using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using service.Models;
using SqlWorker;

namespace service.Utils
{
    public class ConfigWrap
    {
        public ConfigWrap(IConfiguration configuration)
        {
            QuarantineDbConnection = configuration["ConnectionString:QuarantineDb"].ToString().Split('|');
            Radius = int.Parse(configuration["Data:Radius"]);
            MaxDeviceFileSizeBytes = int.Parse(configuration["Data:MaxDeviceFileSizeBytes"]);
            Repository =  new MsSqlDbProvider(this);

        }

        public string[] QuarantineDbConnection { get; }
  
        public int Radius { get; }
        public IDataRepository Repository { get; }
        public int MaxDeviceFileSizeBytes { get; internal set; }
    }
}