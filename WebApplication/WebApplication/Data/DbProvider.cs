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
using Microsoft.Extensions.Configuration;

namespace service.Models
{
    public interface IDataRepository
    {
        void AddOrUpdatePerson(long? phone);
        Task<PersonCacheObject> GetPerson(long? phone );
        Task<PersonCacheObject> GetPerson(string device_id);
        bool AddLocation(string device_id, double lat, double lon, int radius);
        void AddDevicePerson(long phone, string device_id);
        void AddDeviceNotificationToken(string device_id, string token);
        void AddDeviceFile(string device_id, string name);
        NotificationSubscribe[] GetNotifications(bool checkDb =false);
        Task<LocationWithTime[]> GetPersonLocations(long? phone, string device_id);
        Location[] GetPersonsLastLocations();
        Location[] GetZonaLocations();

    }

    public interface ICacheMemoryPersons
    {
        void CheckUpdatePersons();
    }

    public class MsSqlDbProvider : IDataRepository, ICacheMemoryPersons
    {
        readonly static ConcurrentDictionary<string, PersonCacheObject> _cacheDevicePerson = new ConcurrentDictionary<string, PersonCacheObject>();
        readonly static ConcurrentDictionary<long, PersonCacheObject> _cachePhonePerson = new ConcurrentDictionary<long, PersonCacheObject>();
        public static DateTime? LastPersonUpdates { get; private set; }
        public MsSqlDbProvider(IConfiguration configuration)
        {
            QuarantineDbConnection = configuration.GetConnectionString("QuarantineDb").ToString().Split('|');
            Radius = int.Parse(configuration["Data:Radius"]);     
        }
 
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

        public void CheckUpdatePersons()
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
    dnt.token, 
	  fbl.feedback_location.EnvelopeCenter().Lat as lastlat ,
   fbl.feedback_location.EnvelopeCenter().Long as lastlon	,
   fbl.feedback_time
      FROM Person p
      left join [PersonDevice] pd on pd.[phone] = p.phone
      left join DeviceNotificationToken dnt on dnt.device_id = pd.device_id  
	  outer apply(select top(1) fb.* from Feedback fb where fb.person_id = p.phone order by feedback_time desc ) as fbl
END
", (dr) =>
                         {
                             var phone = dr.GetInt64(4);
                             return new
                             {
                                 Person = GetPersons(dr, requestTime, phone),
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

        private PersonCacheObject GetPersons(IDataReader reader, DateTime requestTime, long phoneRes)
        {
            return _cachePhonePerson.AddOrUpdate(phoneRes,
                        (p) => new PersonCacheObject()
                        {
                            Person = GetPersonFromReader(reader),
                            LastUpdate = requestTime,
                            LastLocationUpdateRequest = reader.GetNullableDateTime(9),
                            LastLocation = reader.IsDBNull(7) ? null : new Location()
                            {
                                Lat = (double)reader[7],
                                Lon = (double)reader[8],
                            },
                        },
                        (p, old) =>
                        {
                            if (old.LastUpdate == requestTime)
                                return old;
                            var fb_time = reader.GetNullableDateTime(9);
                            return new PersonCacheObject()
                            {
                                Person = GetPersonFromReader(reader),
                                LastLocation = reader.IsDBNull(7) ? null : new Location()
                                {
                                    Lat = (double)reader[7],
                                    Lon = (double)reader[8],
                                },
                                LastUpdate = requestTime,
                                LastLocationUpdateRequest = fb_time > old.LastLocationUpdateRequest ? fb_time : old.LastLocationUpdateRequest,
                            };
                        });
        }

        public Location[] GetZonaLocations()
        {
            if (_cachePhonePerson.Count == 0) CheckUpdatePersons();
            return _cachePhonePerson.Values.Where(w => w != null && w.Person != null && w.Person.Zone != null).Select(a=>a.Person.Zone).ToArray();
        }
        public Location[] GetPersonsLastLocations()
        {
            if (_cachePhonePerson.Count == 0) CheckUpdatePersons();
            return _cachePhonePerson.Values.Where(w => w != null && w.Person != null && w.LastLocation != null).Select(a => new LocationWithTime()
            {
                Lat = a.LastLocation.Lat,
                Lon = a.LastLocation.Lon,
                Radius = a.LastLocation.Radius
            }).ToArray();
              
        }

        public async Task<LocationWithTime[]> GetPersonLocations(long? phone, string device_id)
        {
            const string sqlCmd = @"
if( ( @device_id is not null AND exists(select top(1) 1 from PersonDevice where device_id = @device_id))
    or  @phone is not null ) 
BEGIN
      SELECT top(100)
	      fbl.feedback_location.EnvelopeCenter().Lat as lastlat ,
       fbl.feedback_location.EnvelopeCenter().Long as lastlon	,
       fbl.feedback_time
          FROM Feedback fbl
          join [PersonDevice] pd on pd.[phone] = fbl.person_id
        where (@phone is null or fbl.person_id = @phone )
    and (@device_id is null or pd.device_id = @device_id)
    order by fbl.feedback_time desc
END";
            try
            {
                DateTime requestTime = DateTime.UtcNow;
                using (var connection = new SqlConnection(GetConnection()))
                {
                    await connection.OpenAsync();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlCmd;
                        cmd.Parameters.AddWithValue("@phone", phone ?? Convert.DBNull);
                        cmd.Parameters.AddWithValue("@device_id", device_id ?? Convert.DBNull);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                  
                            var list = new List<LocationWithTime>();
                            while (await reader.ReadAsync())
                            {
                                var time = reader.GetNullableDateTime(2);
                                list.Add(new LocationWithTime()
                                {
                                    Lat = (double)reader[0],
                                    Lon = (double)reader[1],
                                    UnixUtcTime = time.HasValue ? (long)time.Value.Subtract((new DateTime(1970, 1, 1))).TotalSeconds : (long?)null
                                });
                            }
                            return list.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                HandleExceptionSql(e);
                throw;
            }
        }
        public async Task<PersonCacheObject> GetPerson(long? phone)
        {
            PersonCacheObject person = null;
            if (phone.HasValue && _cachePhonePerson.TryGetValue(phone.Value, out person) && person != null) return person;
            else if (phone.HasValue) return await GetPersonFormDb(phone);
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
                QuarantineStopUnix = !dr.IsDBNull(3) ? (long)dr.GetDateTime(3).Subtract(PersonCacheObject.UnixStart).TotalSeconds : 0
            };
        }

        private async Task<PersonCacheObject> GetPersonFormDb(long? phone = null, string? device_id = null)
        {
            await Task.Delay(1);
            DateTime requestTime = DateTime.UtcNow;

            using (var connection = new MsSqlWorker(GetConnection()))
            {
                return connection.Query(@"
      SELECT 
    isnull(isnull(name_first, name_last), name_patr) as name,
    [quarantine_location].EnvelopeCenter().Lat as lat ,
    [quarantine_location].EnvelopeCenter().Long as lon,
    p.quarantine_stop,
    p.phone,
    pd.device_id,
    dnt.token, 
	  fbl.feedback_location.EnvelopeCenter().Lat as lastlat ,
   fbl.feedback_location.EnvelopeCenter().Long as lastlon	,
   fbl.feedback_time
      FROM Person p
      left join [PersonDevice] pd on pd.[phone] = p.phone
      left join DeviceNotificationToken dnt on dnt.device_id = pd.device_id  
	  outer apply(select top(1) fb.* from Feedback fb where fb.person_id = p.phone order by feedback_time desc ) as fbl
    where (@phone is null or p.phone = @phone )
and (@device_id is null or pd.device_id = @device_id)
", (reader) =>
                {

                    var phoneRes = reader.GetInt64(4);
                    var p = new
                    {
                        Person =GetPersons(reader, requestTime, phoneRes),
                        Phone = phoneRes,
                        DeviceId = reader.GetNullableString(5)
                    };
                    if (p.DeviceId?.Length > 0)
                        _cacheDevicePerson[p.DeviceId] = p.Person;
                    return p.Person;
                }, parameters: new SwParameters()
                {    { "phone",phone  },
                     { "device_id", device_id }
                }).FirstOrDefault();


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
Merge DeviceFile as t
using (select @name, @device_id) as s (name, device_id)
on  t.device_id = s.device_id and t.name = s.name 
when not matched then
insert (device_id, name, recieved)
     values(s.device_id, s.name, GETUTCDATE());
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

        public NotificationSubscribe[] GetNotifications(bool checkDb = false)
        {
            if (NotificationSubscribes == null || checkDb)
                CheckNotifications();
            return NotificationSubscribes;
        }

        public void AddOrUpdatePerson(long? phone)
        {
            DateTime requestTime = DateTime.UtcNow;
            using (var connection = new MsSqlWorker(GetConnection()))
            {
                connection.Exec(@"IF NOT EXISTS(select top(1) 1 from Person where phone = @phone) 
BEGIN
    INSERT INTO [dbo].[Person]
               ([phone]
               ,[name_first]
               ,[quarantine_start]
               ,[quarantine_stop])
         VALUES
               (@phone
               ,cast(@phone as nvarchar(200))      
               ,GETUTCDATE()
               ,DATEADD(DAY, 14, GETUTCDATE()))
END
else BEGIN
	update Person 
	set quarantine_start = GETUTCDATE()
	, quarantine_stop = DATEADD(DAY, 14, GETUTCDATE())
	where phone =@phone
END
                                    ", parameters: new SwParameters
                                            {
                                                { "phone",phone.Value  },
                                            });
              
            }
            if (_cachePhonePerson.Count != 0)
            {
               var res = GetPersonFormDb(phone).Result;
            }
           
        }
    }
}
