using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using address_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using service.Models;
using SqlWorker;

namespace address_service.Utils
{
    public class ConfigWrap
    {
        public ConfigWrap(IConfiguration configuration)
        {
            QuarantineDbConnection = configuration["ConnectionString:QuarantineDb"].ToString().Split('|');
            Radius = int.Parse(configuration["Data:Radius"]);
            Repository =  new MsSqlDbProvider(this);

        }

        public string[] QuarantineDbConnection { get; }
  
        public int Radius { get; }
        public IDataRepository Repository { get; }
    }
}