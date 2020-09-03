using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Primitives;
using MimeMapping;
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
                fixture.Register(() => new MediaTypeHeaderValue(KnownMimeTypes.Csv)
                {
                    Boundary = fixture.Create<StringSegment>()
                });
                return fixture;
            })
        { }
    }

}