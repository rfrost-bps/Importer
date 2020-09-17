using System;
using System.Diagnostics.CodeAnalysis;
using EventStore.ClientAPI;
using Importer.Repository;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public sealed class StartupShould
    {

        [Theory]
        [InlineData(typeof(ISwaggerProvider))]
        [InlineData(typeof(IEventStoreConnection))]
        [InlineData(typeof(IRebalanceRepository))]
        public void ConfigureServices(Type service)
        {
            var host = WebHost.CreateDefaultBuilder<Startup>(null)
                .ConfigureAppConfiguration((context, builder) => builder.AddJsonFile("appsettings.json"))
                .Build();
            Assert.NotNull(host.Services.GetService(service));
        }
    }
}