using Microsoft.Extensions.Logging;
using service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace service.BackgroundService
{
    public class UpdatePersonInMemoryBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private ICacheMemoryPersons _cacheMemoryPersons;
        private ILogger<UpdatePersonInMemoryBackgroundService> _logger;

        public UpdatePersonInMemoryBackgroundService(ICacheMemoryPersons cacheMemoryPersons, ILogger<UpdatePersonInMemoryBackgroundService> logger)
        {
            _cacheMemoryPersons = cacheMemoryPersons;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
               

                try
                {
                    _cacheMemoryPersons.CheckUpdatePersons();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error");
                }
                await Task.Delay(60000);
            }

        }
    }
}
