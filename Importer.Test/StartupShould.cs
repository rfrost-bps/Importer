using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Testing;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public sealed class StartupShould
    {

        [Theory]
        [InlineData(typeof(ISwaggerProvider))]
        public void ConfigureServices(Type service)
        {
            var factory = new WebApplicationFactory<Startup>();
            Assert.NotNull(factory.Services.GetService(service));
        }
    }
}