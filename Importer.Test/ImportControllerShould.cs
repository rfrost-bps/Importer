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
using Importer.Controllers;
using Importer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Xunit;
using Formatting = Newtonsoft.Json.Formatting;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    [UseReporter(typeof(DiffReporter))]
    public class ImportControllerShould: IClassFixture<WebApplicationFactory<Startup>>, IDisposable
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly ILoggerFactory _loggerFactory;

        public ImportControllerShould(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }


        [Fact]
        public void Exist()
        {
            Assert.IsAssignableFrom<ControllerBase>(new ImportController(_loggerFactory.CreateLogger<ImportController>()));
        }

        [Theory, AutoData]
        public async Task Import(string correlationKey)
        {
            var client = _factory.CreateClient();
            using var content = new MultipartContent("mixed","========");
            content.Headers.Add("correlationKey", correlationKey);

            using var portfolioContent = new MultipartContent("mixed","++++++++");
            portfolioContent.Headers.Add("AggregateType", "Portfolio");

            var template = await File.ReadAllTextAsync("Household.xml");
            using var templateStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(template)));
            templateStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            templateStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("template") { Name = "Household" };
            portfolioContent.Add(templateStream);

            var data = await File.ReadAllTextAsync("Household.txt");
            using var dataStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            dataStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/csv");
            dataStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("data") { Name = "Household" };
            portfolioContent.Add(dataStream);

            using var streamContent = new StreamContent(await portfolioContent.ReadAsStreamAsync());
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/mixed");
            streamContent.Headers.Add("AggregateType", "Portfolio");
            content.Add(streamContent);
            var response = await client.PostAsync("/api/Import", content);
            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ValidateCorrelationKey(string correlationKey)
        {
            var client = _factory.CreateClient();
            var content = new MultipartContent();
            content.Headers.Add("correlationKey", correlationKey);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => client.PostAsync("/api/Import", content));
            Assert.Equal("correlationKey", ex.ParamName);
        }

        [Fact]
        public async Task ImportStream()
        {
            using var modelContent = new MultipartContent("mixed","++++++++");
            modelContent.Headers.Add("AggregateType", "Model");

            var template = await File.ReadAllTextAsync("Model.xml");
            using var templateStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(template)));
            templateStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            templateStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("template") { Name = "Model" };
            modelContent.Add(templateStream);

            var data = await File.ReadAllTextAsync("Model.txt");
            using var dataStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            dataStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/csv");
            dataStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("data") { Name = "Model" };
            modelContent.Add(dataStream);

            var section = new MultipartSection
            {
                Body = await modelContent.ReadAsStreamAsync()
            };

            var sut = new ImportController(_loggerFactory.CreateLogger<ImportController>());
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
            var client = _factory.CreateClient();
            using var content = new StringContent("error");
            content.Headers.Add("correlationKey", correlationKey);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync("/api/Import", content));
            Assert.Equal("Expected multipart content type but, content type was text/plain; charset=utf-8", ex.Message);
        }
    }
}