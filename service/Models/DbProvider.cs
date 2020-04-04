using service.Utils;
using SqlWorker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;

namespace service.Models
{
    public interface IDataRepository
    {
        Task<PersonCacheObject> GetPerson(long? phone );
        Task<PersonCacheObject> GetPerson(string device_id);
        bool AddLocation(string device_id, double lat, double lon, int radius);
        void AddDevicePerson(long phone, string device_id);
        void AddDeviceNotificationToken(string device_id, string token);
        void AddDeviceFile(string device_id, string name);
        NotificationSubscribe[] GetNotifications();

    }
    public class MsSqlDbProvider : IDataRepository
    {
        public MsSqlDbProvider(ConfigWrap config)
        {
            QuarantineDbConnection = config.QuarantineDbConnection;
            Radius = config.Radius;
            if (_taskUpdatePhoneMemory == null)
                lock (_lock)
                {
                    if (_taskUpdatePhoneMemory == null)
                    {
                        _taskUpdatePhoneMemory = Task.Run(() =>
                        {
                            while (true)
                            {
                                try
                                {
                                    CheckUpdatePhones();
                                }catch(Exception e)
                                {
                                    // this ignore
                                }
                                try
                                {
                                    CheckNotifications();
                                }
                                catch (Exception e)
                                {
                                    // this ignore
                                }
                              
                                Task.WaitAny(Task.Delay(5000));

                            }
                        });
                    }
                }
        }
        readonly static object _lock = new object();
        readonly static ConcurrentDictionary<string, PersonCacheObject> _cacheDevicePerson = new ConcurrentDictionary<string, PersonCacheObject>();
        readonly static ConcurrentDictionary<long, PersonCacheObject> _cachePhonePerson = new ConcurrentDictionary<long, PersonCacheObject>();
        private static Task _taskUpdatePhoneMemory;
        public static DateTime? LastPersonUpdates { get; private set; }

 
        private int QuarantineDbConnectionIndex;


        private string[] QuarantineDbConnection { get; }
        public int Radius { get; }
        public static NotificationSubscribe[] NotificationSubscribes { get; private set; }
        public static DateTime? LastNotificationUpdates { get; private set; }

        public string GetConnection()
        {
            return QuarantineDbConnection[QuarantineDbConnectionIndex];
        }
        internal void NextConnection()
        {
            if (QuarantineDbConnectionIndex < QuarantineDbConnection?.Length - 1)
                QuarantineDbConnectionIndex += 1;
            else QuarantineDbConnectionIndex = 0;
        }
        public void HandleExceptionSql(Exception e)
        {
            if (e is System.Data.SqlClient.SqlException || e is ObjectDisposedException)
                NextConnection();
        }

        private void CheckUpdatePhones()
        {
            if (!LastPersonUpdates.HasValue || LastPersonUpdates < DateTime.UtcNow.AddMinutes(-1))
            {
                var requestTime = DateTime.UtcNow;
                using (var connection = new MsSqlWorker(GetConnection()))
                {
                    var persons = connection.Query(
                         @"
IF(@lastTime is null or EXISTS(SELECT 1 FROM [ServiceTimestamp]
  WHERE [service_code] = 'Person' and last_start > @lastTime))
BEGIN
    SELECT 
    isnull(isnull(name_first, name_last), name_patr) as name,
    [quarantine_location].EnvelopeCenter().Lat as lat ,
    [quarantine_location].EnvelopeCenter().Long as lon,
    p.quarantine_stop,
    p.phone,
    pd.device_id,
    dnt.token
      FROM Person p
      left join [PersonDevice] pd on pd.[phone] = p.phone
      left join DeviceNotificationToken dnt on dnt.device_id = pd.device_id
END
", (dr) =>
                         {
                             var phone = dr.GetInt64(4);
                             return new
                             {
                                 Person = _cachePhonePerson.AddOrUpdate(phone,
                                 (p)=> new PersonCacheObject() { Person = GetPersonFromReader(dr), LastUpdate = requestTime },
                                 (p, old)=>
                                 {
                                     if (old.LastUpdate == requestTime)
                                         return old;
                                     return new PersonCacheObject()
                                     {
                                         Person = GetPersonFromReader(dr),
                                         LastUpdate = requestTime,
                                         LastLocationUpdateRequest = old.LastLocationUpdateRequest,
                                     };
                                 }),
                                 Phone = phone,
                                 DeviceId = dr.GetNullableString(5)
                             };
                         }, parameters: new SwParameters
                                            {
                                                { "@lastTime", LastPersonUpdates },
                                            }).ToArray();
                    if (persons?.Length > 0)
                    {
                        _cacheDevicePerson.Clear();
                       
                        foreach (var p in persons)
                        {
                            if (p.DeviceId?.Length > 0)
                                _cacheDevicePerson[p.DeviceId] = p.Person;
                        }
                        foreach(var delPhone in _cachePhonePerson.Where(w=>w.Value.LastUpdate != requestTime).Select(a=>a.Key))
                        {
                            _cachePhonePerson.TryRemove(delPhone, out var _);
                        }
                    }
                    LastPersonUpdates = DateTime.UtcNow.AddSeconds(-1);
                }
            }
        }
        public async Task<PersonCacheObject> GetPerson(long? phone)
        {
            PersonCacheObject person = null;
            if (phone.HasValue && _cachePhonePerson.TryGetValue(phone.Value, out person) && person != null) return person;
            else if (phone.HasValue && _cachePhonePerson.Count == 0) return _cachePhonePerson[phone.Value] = await GetPersonFormDb(phone);
            return null;
        }
        public async Task<PersonCacheObject> GetPerson(string device_id)
        {
            PersonCacheObject person = null;
            if (device_id?.Length > 0 && _cacheDevicePerson.TryGetValue(device_id, out person) && person != null) return person;
            else if (device_id?.Length > 5 && _cacheDevicePerson.Count == 0) return await GetPersonFormDb(null, device_id);
            return null;
        }

        public void AddDevicePerson(long phone, string device_id)
        {
            try
            {
                using (var connection = new MsSqlWorker(GetConnection()))
                {
                    connection.Exec(@"
                                      insert into PersonDevice (phone, device_id)
                                      values(@phone, @device_id)
                                    ", parameters: new SwParameters
                                            {
                                                { "phone",phone  },
                                                { "device_id", device_id }
                                            });
                    if(_cachePhonePerson.Count != 0)
                        _cacheDevicePerson[device_id] = _cachePhonePerson[phone];
                }
            }
            catch (Exception e)
            {
                HandleExceptionSql(e);
                throw;
            }
        }

        public bool AddLocation(string device_id, double lat, double lon, int radius)
        {
            try
            {
                if (_cacheDevicePerson.TryGetValue(device_id, out var person))
                {
                    using (var connection = new MsSqlWorker(GetConnection()))
                    {
                        connection.Exec(
                             @"
 insert into [Feedback] (id, person_id,external_reference, feedback_time, feedback_type, feedback_location )
                            select newid()
                        , (select top(1) phone from [PersonDevice] where device_id = @device_id)
                        , @device_id
                        , GETUTCDATE()
                        , 'device'
                        , 	geography::STPointFromText(@locationText, 4326)
						    .STBuffer(@radius)
", parameters: new SwParameters
                                                {
                                                { "device_id", device_id },
                                                { "locationText", $"POINT( {lon.ToString(CultureInfo.InvariantCulture)} {lat.ToString(CultureInfo.InvariantCulture)})" },
                                                { "radius", radius < 0 ? 0 : (radius > Radius ? Radius : radius) }
                                                });
                        person.LastLocationUpdateRequest = DateTime.UtcNow;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                HandleExceptionSql(e);
                throw;
            }
        }
        private PersonObject GetPersonFromReader(IDataReader dr)
        {
            return new PersonObject()
            {
                Name = dr.GetNullableString(0),
                Zone = dr.IsDBNull(1) ? null : new Location()
                {
                    Lat = (double)dr[1],
                    Lon = (double)dr[2],
                    Radius = Radius
                },
                QuarantineStopUnix = !dr.IsDBNull(3) ? (long)dr.GetDateTime(3).Subtract(PersonCacheObject.UnixStart).TotalSeconds : 0,
                Token = dr.GetNullableString(6)
            };
        }

        private async Task<PersonCacheObject> GetPersonFormDb(long? phone = null, string? device_id = null)
        {
            DateTime requestTime = DateTime.UtcNow;
            using (var connection = new SqlConnection(GetConnection()))
            {
                if (connection.State != ConnectionState.Open) await connection.OpenAsync();
                using (var cmd = new SqlCommand(@"
    SELECT 
    isnull(isnull(name_first, name_last), name_patr) as name,
    [quarantine_location].EnvelopeCenter().Lat as lat ,
    [quarantine_location].EnvelopeCenter().Long as lon,
    p.quarantine_stop,
    p.phone,
    pd.device_id,
    dnt.token
      FROM Person p
      left join [PersonDevice] pd on pd.[phone] = p.phone
      left join DeviceNotificationToken dnt on dnt.device_id = pd.device_id
    where (@phone is null or p.phone = @phone )
and (@device_id is null or pd.device_id = @device_id)
"))
                {
                    cmd.Parameters.AddWithValue("@lastTime", LastPersonUpdates);
                    PersonCacheObject personResult = null;
                    using(var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                    {
                        while (await reader.ReadAsync())
                        {
                            var phoneRes = reader.GetInt64(4);
                            var p = new
                            {
                                Person = _cachePhonePerson.AddOrUpdate(phoneRes,
                                (p) => new PersonCacheObject() { Person = GetPersonFromReader(reader), LastUpdate = requestTime },
                                (p, old) =>
                                {
                                    if (old.LastUpdate == requestTime)
                                        return old;
                                    return new PersonCacheObject()
                                    {
                                        Person = GetPersonFromReader(reader),
                                        LastUpdate = requestTime,
                                        LastLocationUpdateRequest = old.LastLocationUpdateRequest,
                                    };
                                }),
                                Phone = phoneRes,
                                DeviceId = reader.GetNullableString(5)
                            };
                            if (p.DeviceId?.Length > 0)
                                _cacheDevicePerson[p.DeviceId] = p.Person;
                            personResult = p.Person;
                        }
                        return personResult;
                    }
                }
            }
        }

        public void AddDeviceNotificationToken(string device_id, string token)
        {
            try
            {
                using (var connection = new MsSqlWorker(GetConnection()))
                {
                    connection.Exec(
 @"
Merge DeviceNotificationToken t
using (select @token, @device_id) as s (token, device_id)
on  t.device_id = s.device_id
when matched then and t.token != s.token
update set token = s.token
when not matched then
insert into  (device_id, token)
     values(s.device_id, s.token)
                                    ", parameters: new SwParameters
                                            {
                                                { "token",token  },
                                                { "device_id", device_id }
                                            });
                    if (_cachePhonePerson.Count != 0)
                        _cacheDevicePerson[device_id].Person.Token = token;
                }
            }
            catch (Exception e)
            {
                HandleExceptionSql(e);
                throw;
            }
        }

        public void AddDeviceFile(string device_id, string name)
        {
            try
            {
                using (var connection = new MsSqlWorker(GetConnection()))
                {
                    connection.Exec(
 @"
Merge DeviceFile t
using (select @name, @device_id) as s (name, device_id)
on  t.device_id = s.device_id and t.name = s.name 
when not matched then
insert into  (device_id, name, recieved)
     values(s.device_id, s.name, GETUTCNOW())
                                    ", parameters: new SwParameters
                                            {
                                                { "name", name },
                                                { "device_id", device_id }
                                            });
                }
            }
            catch (Exception e)
            {
                HandleExceptionSql(e);
                throw;
            }
        }

        /* select 
	  Name,
	  start_time,
	  end_time, 
	  interval 
  from NotificationSubscribe*/
        private void CheckNotifications()
        {
            if (!LastNotificationUpdates.HasValue || LastNotificationUpdates < DateTime.UtcNow.AddMinutes(-1))
            {
                var requestTime = DateTime.UtcNow;
                using (var connection = new MsSqlWorker(GetConnection()))
                {
                    NotificationSubscribes = connection.Query(
                         @"
IF(@lastTime is null or EXISTS(SELECT 1 FROM [ServiceTimestamp]
  WHERE [service_code] = 'NotificationSubscribe' and last_start > @lastTime))
BEGIN
  select 
	  Name,
	  start_time,
	  end_time, 
	  interval,
      topic,
      message_template,
      type
  from NotificationSubscribe
  where (end_time is null or end_time <GETUTCDATE());
End
", (dr) => new NotificationSubscribe() { Name =dr.GetString(0),
                         StartTime = dr.GetNullableDateTime(1),
                         EndTime = dr.GetNullableDateTime(2),
                         Interval =TimeSpan.FromMinutes( dr.GetNullableInt32(3) ?? 30),
                         Topic = dr.GetNullableString(4),
                         MessageTemplate = dr.GetNullableString(5),
                         NotificationType = (NotificationTypeEnum)( dr.GetNullableInt32(6) ?? 0)}
                         , parameters: new SwParameters
                                            {
                                                { "@lastTime", LastPersonUpdates },
                                            })?.ToArray() ?? new NotificationSubscribe[0];
                    
                    LastNotificationUpdates = DateTime.UtcNow.AddSeconds(-1);
                }
            }
        }

        public NotificationSubscribe[] GetNotifications()
        {
            if (NotificationSubscribes == null)
                CheckNotifications();
            return NotificationSubscribes;
        }
    }
}
