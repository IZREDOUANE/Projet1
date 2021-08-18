using Azure.Api.Data;
using Azure.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Azure.Api.Repository.Monitoring
{
    public class MonitoringRepository : DocumentStoreRepository<MonitoringFlux>, IMonitoringRepository
    {
        public MonitoringRepository(DocumentStoreContext dbContext) : base(dbContext)
        {
        }
    }
}
