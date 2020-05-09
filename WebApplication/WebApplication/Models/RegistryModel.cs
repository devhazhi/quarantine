using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class RegistryModel
    {
        public string DeviceId { get; set; }
        public bool AddQuarantine { get; set; }
        public string ErrorInfo { get; set; }
    }
}
