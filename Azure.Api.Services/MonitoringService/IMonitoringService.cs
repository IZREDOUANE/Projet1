using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Azure.Api.Data.Models;

namespace Azure.Api.Services.Monitoring
{
    public interface IMonitoringService
    {
        Task<MonitoringFlux> GetByIdAsync(params object[] keyValues);
        Task<MonitoringFlux> GetByIdAsync(int id);
        IEnumerable<MonitoringFlux> FindAll();
        IEnumerable<MonitoringFlux> FindAll(Expression<Func<MonitoringFlux, bool>> predicate);
        Task<MonitoringFlux> Add(MonitoringFlux entity);
        Task Delete(MonitoringFlux entity);
        Task<bool> Update(MonitoringFlux entity);
        Task<MonitoringFlux> GetByAsync(Expression<Func<MonitoringFlux, bool>> predicate);
    }
}
