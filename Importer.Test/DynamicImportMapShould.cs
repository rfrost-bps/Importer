using System.Diagnostics.CodeAnalysis;
using System.IO;
using Blaze.Domain.Impl;
using CsvHelper.Configuration;
using Importer.Controllers;
using Importer.Helpers;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class DynamicImportMapShould
    {
        [Fact]
        public void Exist()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            using var rdr = new StringReader(File.ReadAllText("Model.xml"));
            var template = (Template)ImportController.TemplateSerializer.Deserialize(rdr);
            var map = new DynamicImportMap<Model>(template, loggerFactory.CreateLogger<DynamicImportMap<Model>>());
            Assert.IsAssignableFrom<ClassMap<Model>>(map);
        }
    }
}