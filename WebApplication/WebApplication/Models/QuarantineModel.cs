using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class QuarantineModel
    {
        //public MenuItem[] MenuItems { get; set; }
        public PersonModel Person { get; set; }
        public string DeviceId { get; set; }
        public string StateSendLocation { get; set; }
    }
}
