using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace service.Models
{
    public class NotificationSubscribeInfoResponce : Responce
    {
        public string[] Topics { get; set; }        
    }

    public class NotificationSubscribe
    {
        public string Name { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Interval { get; set; }
        public string MessageTemplate { get; internal set; }
        public string Topic { get; internal set; }
        public NotificationTypeEnum NotificationType { get; internal set; }
    }

    public enum NotificationTypeEnum
    {
        Info,
        Warning,
        TakePhoto,
        QuarantineVioalation
    }
}
