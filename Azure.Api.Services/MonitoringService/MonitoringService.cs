using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure.Api.Data.Models;
using Azure.Api.Repository.Monitoring;

namespace Azure.Api.Services.Monitoring
{
    public class MonitoringService: IMonitoringService
    {
        private readonly IMonitoringRepository _monitoringRepository;

        public MonitoringService(IMonitoringRepository monitoringRepository)
        {
            _monitoringRepository = monitoringRepository;
        }

        public Task<MonitoringFlux> GetByIdAsync(params object[] keyValues)
        {
            return _monitoringRepository.GetByIdAsync(keyValues);
        }

        public Task<MonitoringFlux> GetByIdAsync(int id)
        {
            return _monitoringRepository.GetByIdAsync(id);
        }

        public IEnumerable<MonitoringFlux> FindAll()
        {
            return _monitoringRepository.Query()
                .Select(s => new MonitoringFlux { Date = s.Date, Erreur = s.Erreur, Id = s.Id, IsSuccess = s.IsSuccess, Nom = s.Nom, Sens = s.Sens, Source = s.Source, Type = s.Type })
                .OrderByDescending(m => m.Id)
            .ToList();
        }

        public IEnumerable<MonitoringFlux> FindAll(Expression<Func<MonitoringFlux, bool>> predicate)
        {
            return _monitoringRepository.List(predicate).ToList();
        }

        public Task<MonitoringFlux> Add(MonitoringFlux entity)
        {
            return _monitoringRepository.Add(entity);
        }

        public async Task Delete(MonitoringFlux entity)
        {
            await _monitoringRepository.Delete(entity);
        }

        public Task<bool> Update(MonitoringFlux entity)
        {
            return _monitoringRepository.Update(entity);
        }

        public Task<MonitoringFlux> GetByAsync(Expression<Func<MonitoringFlux, bool>> predicate)
        {
            return _monitoringRepository.GetByAsync(predicate);
        }
    }
}
