using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using qurantine.service;

namespace WebApplication.Models
{
    public class PersonModel
    {
        private PersonObject _model;

        public PersonModel(PersonObject pob)
        {
            _model = pob;
        }
        [DisplayName("Имя")]
        public string Name => _model?.Name;
        [DisplayName("Дата окончания")]
        public string QuarantineStopInfo => _model?.QuarantineStopUnix > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(_model.QuarantineStopUnix).ToLocalTime().ToString("D", new CultureInfo("ru-RU")) : "Нет карантина";
        [DisplayName("Координаты")]
        public string ZonaCooordinate => _model?.Zone != null ? _model.Zone.Lat.ToString() + " " + _model.Zone.Lon.ToString() : "Неизвестно";

    
        public Location Zona => _model?.Zone ?? new Location();
    }
}
