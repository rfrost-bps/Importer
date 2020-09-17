using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Importer.Repository;
using Microsoft.Extensions.Primitives;
using MimeMapping;
using Moq;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(
            () =>
            {
                var fixture = new Fixture();
                fixture.Customize(new AutoMoqCustomization())
                    .Register(() => new MediaTypeHeaderValue(KnownMimeTypes.Csv)
                    {
                        Boundary = fixture.Create<StringSegment>()
                    });
                var moqConn = fixture.Freeze<Mock<IEventStoreConnection>>();
                moqConn.Setup(c => c.AppendToStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<IEnumerable<EventData>>(),
                    It.IsAny<UserCredentials>())).ReturnsAsync(
                    (string stream, long expected, IEnumerable<EventData> data, UserCredentials credentials)
                    => new WriteResult(expected + data.Count(), new Position()));
                fixture.Register<IRebalanceRepository>(() => new RebalanceRepository(moqConn.Object));
                fixture.Freeze<IRebalanceRepository>();
                return fixture;
            })
        { }
    }

}