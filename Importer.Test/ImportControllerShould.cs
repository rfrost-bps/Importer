using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Reporters;
using AutoFixture.Xunit2;
using Blaze.Domain.Impl;
using Importer.Controllers;
using Importer.Models;
using Importer.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Formatting = Newtonsoft.Json.Formatting;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    [UseReporter(typeof(DiffReporter))]
    public class ImportControllerShould: IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public ImportControllerShould()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }


        [Theory, AutoMoqData]
        public void Exist(IRebalanceRepository repo)
        {
            Assert.IsAssignableFrom<ControllerBase>(new ImportController(_loggerFactory.CreateLogger<ImportController>(), repo));
        }

        [Theory, AutoMoqData]
        public async Task Import(string correlationKey)
        {
            var factory = new WebApplicationFactory<Startup>();
            var moqRepo = new Mock<IRebalanceRepository>();
            var client = factory.WithWebHostBuilder(bldr =>
            {
                bldr.ConfigureServices(services =>
                {
                    services.AddSingleton(moqRepo.Object)
                        .AddTransient(provider => moqRepo.Object);
                });
            }).CreateClient();
            using var content = new MultipartContent("mixed","========");
            content.Headers.Add("correlationKey", correlationKey);

            using var portfolioContent = new MultipartContent("mixed","++++++++");
            using var templateStream = await "Household.xml".AsStreamContent();
            using var dataStream = await "Household.txt".AsStreamContent();
            portfolioContent.AddContent(
                "Household",
                templateStream,
                dataStream);

            using var streamContent = new StreamContent(await portfolioContent.ReadAsStreamAsync());
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed");
            streamContent.Headers.Add("AggregateType", "Portfolio");
            content.Add(streamContent);
            moqRepo.Setup((lst, id) => Task.FromResult((long) lst.Count));
            var response = await client.PostAsync("/api/Import", content);
            Assert.True(response.IsSuccessStatusCode);
            moqRepo.Verify<Portfolio>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValidateCorrelationKey(string correlationKey)
        {
            var factory = new WebApplicationFactory<Startup>();
            var moqRepo = new Mock<IRebalanceRepository>();
            var client = factory.WithWebHostBuilder(bldr =>
            {
                bldr.ConfigureServices(services =>
                {
                    services.AddSingleton(moqRepo.Object)
                        .AddTransient(provider => moqRepo.Object);
                });
            }).CreateClient();
            var content = new MultipartContent();
            content.Headers.Add("correlationKey", correlationKey);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.PostAsync("/api/Import", content));
            Assert.Equal("correlationKey", ex.ParamName);
        }

        [Theory]
        [InlineData("Model", 4)]
        public async Task ImportStream(string aggregateType, long recordCount)
        {
            var moqRepo = new Mock<IRebalanceRepository>();
            moqRepo.Setup((lst,  id) =>
            {
                Assert.Equal(recordCount, lst.Count);
                return Task.FromResult(recordCount);
            });
            using var modelContent = new MultipartContent("mixed","++++++++");
            using var templateStream = await $"{aggregateType}.xml".AsStreamContent();
            using var dataStream = await $"{aggregateType}.txt".AsStreamContent();
            modelContent.AddContent(
                aggregateType,
                templateStream,
                dataStream);

            var section = new MultipartSection
            {
                Body = await modelContent.ReadAsStreamAsync()
            };

            var sut = new ImportController(_loggerFactory.CreateLogger<ImportController>(), moqRepo.Object);
            await sut.ImportStream(section);
        }

        [Fact]
        public async Task ReadTemplate()
        {
            var modelText = await File.ReadAllTextAsync("Model.xml");
            await using var modelTextStream = new MemoryStream(Encoding.UTF8.GetBytes(modelText));
            
            var section = new MultipartSection
            {
                Body = modelTextStream,
                Headers = new Dictionary<string,StringValues>
                {
                    {"Content-Type", new StringValues("text/xml")},
                }
            };

            var template = await ImportController.ReadTemplate(section);

            Assert.IsAssignableFrom<Template>(template);
            Approvals.Verify(JsonConvert.SerializeObject(template, Formatting.Indented));
        }

        [Theory, AutoData]
        public async Task ThrowFromIndexWhenRequestContentIsNotMultiPart(string correlationKey)
        {
            var factory = new WebApplicationFactory<Startup>();
            var moqRepo = new Mock<IRebalanceRepository>();
            var client = factory.WithWebHostBuilder(bldr =>
            {
                bldr.ConfigureServices(services =>
                {
                    services.AddSingleton(moqRepo.Object)
                        .AddTransient(provider => moqRepo.Object);
                });
            }).CreateClient();
            using var content = new StringContent("error");
            content.Headers.Add("correlationKey", correlationKey);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync("/api/Import", content));
            Assert.Equal("Expected multipart content type but, content type was text/plain; charset=utf-8", ex.Message);
        }

        [Theory, AutoData]
        public void CreateEventDataFromEntity(Model entity)
        {
            var eventData = ImportController.From(entity);
            Assert.Equal(entity.Id, eventData.EventId);
            Assert.Equal("Model", eventData.Type);
            Assert.NotNull(eventData.Metadata);
            Assert.Equal(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity)), eventData.Data);
        }

        [Fact]
        public async Task ThrowFromImportStreamWhenRecordTypeIsNotADomainObject()
        {
            var moqRepo = new Mock<IRebalanceRepository>();
            moqRepo.Setup((lst,  id) =>Task.FromResult((long)lst.Count));
            using var modelContent = new MultipartContent("mixed","++++++++");
            using var templateStream = await $"Model.Bad.xml".AsStreamContent();
            using var dataStream = await $"Model.txt".AsStreamContent();
            modelContent.AddContent(
                "Model",
                templateStream,
                dataStream);

            var section = new MultipartSection
            {
                Body = await modelContent.ReadAsStreamAsync()
            };

            var sut = new ImportController(_loggerFactory.CreateLogger<ImportController>(), moqRepo.Object);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.ImportStream(section));
            Assert.Equal("Cannot import records into object of type BadModel", ex.Message);
        }
    }
}