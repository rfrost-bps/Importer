using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze.Domain.Impl;
using EventStore.ClientAPI;

namespace Importer.Repository
{
    public interface IRebalanceRepository
    {
        Task<long> SaveAsync<TEntity>(IList<TEntity> entities, Guid correlationId, Func<TEntity, EventData> eventFactory);
    }
}