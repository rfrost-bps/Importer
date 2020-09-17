using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using Blaze.Domain.Impl;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Importer.Repository;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class RebalanceRepositoryShould
    {
        [Theory, AutoMoqData]
        public void Exist(IEventStoreConnection connection)
        {
            var sut = new RebalanceRepository(connection);
            Assert.IsAssignableFrom<IRebalanceRepository>(sut);
        }

        [Theory, AutoMoqData]
        public async Task SavePortfoliosAsync(IEventStoreConnection connection, [Frozen]Mock<IEventStoreConnection> moq)
        {
            var sut = new RebalanceRepository(connection, 3);
            using var cancellation = new CancellationTokenSource(1000);
            var fixture = new Fixture();
            var portfolios = fixture.Build<Portfolio>()
                .Without(p => p.Id)
                .CreateMany(5)
                .ToList();
            var saved = await sut.SaveAsync<Portfolio>(portfolios, default, p =>
            {
                p.Id = Guid.NewGuid();
                return new EventData(
                    p.Id,
                    "Portfolio",
                    true,
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(p)),
                    null);
            });
            Assert.Equal(5, saved);
            foreach (var portfolio in portfolios) Assert.NotEqual(default, portfolio.Id);
            moq.Verify(c => c.AppendToStreamAsync(
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<EventData>>(),
                It.IsAny<UserCredentials>()));
        }
    }
}