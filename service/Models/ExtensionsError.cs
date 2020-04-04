using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace service.Models
{
    public static class ExtensionsError
    {
        public static Responce getResponce(this Exception e)
        {
            return new Responce() { ErrorCode = ErrorCode.NotSupport, Error = e.Message };
        }
        public static Responce getResponce(this ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.NotQuarantine:
                    return new Responce()
                    {
                        Error = "Вас нет в карантине или карантин закончен для вас",
                        ErrorCode = ErrorCode.NotQuarantine
                    };
                case ErrorCode.FormatDeviceIdNotSupport:
                    return new Responce()
                    {
                        Error = "Идентификатор вашего token не соответствует правилам безопасности",
                        ErrorCode = ErrorCode.FormatDeviceIdNotSupport
                    };
                case ErrorCode.FormatTokenNotSupport:
                    return new Responce()
                    {
                        Error = "Идентификатор вашего token не соответствует правилам безопасности",
                        ErrorCode = ErrorCode.FormatDeviceIdNotSupport
                    };
                case ErrorCode.CoordinateFailed:
                    return new Responce() { IsOk = false, Error = "Координаты неизвестны", ErrorCode = ErrorCode.CoordinateFailed };
                default: return new Responce() { ErrorCode = errorCode };
            }

        }

        public static bool Check(this PersonCacheObject person)
        {
            if (person == null || person.HasQuarantineStop) return false;
            return true;
        }

        public static bool CheckFormatDeviceId(this string device_id)
        {
            return device_id != null && device_id.Length >= 5;
        }

    } 

}
