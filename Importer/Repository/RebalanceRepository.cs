using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Importer.Repository
{
    public class RebalanceRepository : IRebalanceRepository
    {
        private readonly IEventStoreConnection _connection;
        private readonly int _pageSize;

        public RebalanceRepository(IEventStoreConnection connection, int pageSize = 4096)
        {
            _connection = connection;
            _pageSize = pageSize;
        }

        public async Task<long> SaveAsync<TEntity>(IList<TEntity> entities, Guid correlationId, Func<TEntity, EventData> eventFactory)
        {
            var sent = 0;
            for (var page = 0; page < 1 + entities.Count() / _pageSize; ++page)
            {
                var events = entities
                    .Skip(page * _pageSize)
                    .Take(_pageSize)
                    .Select(eventFactory)
                    .ToList();
                await _connection.AppendToStreamAsync("Portfolio", ExpectedVersion.Any, events);
                sent += events.Count;
            }
                
            return sent;
        }
    }
}