using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture.Xunit2;
using Blaze.Domain.Impl;
using CsvHelper.Configuration;
using Importer.Helpers;
using Importer.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class TemplateShould
    {
        [Theory]
        [InlineData("Model")]
        [InlineData("Portfolio")]
        [InlineData("TaxLot")]
        [InlineData("Security")]
        [InlineData("Order")]
        public void CreateMap(string typeName)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var template = new Template
            {
                Detail = new Detail
                {
                    Delimiter = Delimiter.Comma,
                    Description = "Test",
                    HasHeaders = true,
                    MappingType = MappingType.ColumnOrder,
                    Name = "Test"
                },
                ImportTemplateItems = new ImportTemplateItems
                {
                    Item = new[]
                    {
                        new Item {ColumnNumber = 1, AtomField = "Id", AtomTable = typeName}
                    }.ToList()
                }
            };
            var map = template.CreateMap(loggerFactory.CreateLogger<DynamicImportMap<Model>>());
            Assert.IsAssignableFrom<ClassMap>(map);
        }

        [Fact]
        public void ThrowFromCreateMapIfMultipleTables()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var template = new Template
            {
                Detail = new Detail
                {
                    Delimiter = Delimiter.Comma,
                    Description = "Test",
                    HasHeaders = true,
                    MappingType = MappingType.ColumnOrder,
                    Name = "Test"
                },
                ImportTemplateItems = new ImportTemplateItems
                {
                    Item = new[]
                    {
                        new Item {ColumnNumber = 1, AtomField = "Id", AtomTable = "Model"},
                        new Item {ColumnNumber = 2, AtomField = "Id", AtomTable = "Portfolio"}
                    }.ToList()
                }
            };
            var ex = Assert.Throws<InvalidOperationException>(() => template.CreateMap(loggerFactory.CreateLogger<DynamicImportMap<Model>>()));
            Assert.Equal("Import into multiple tables is not supported.", ex.Message);
        }

        [Fact]
        public void ThrowFromCreateMapIfTableIsUnsupported()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var template = new Template
            {
                Detail = new Detail
                {
                    Delimiter = Delimiter.Comma,
                    Description = "Test",
                    HasHeaders = true,
                    MappingType = MappingType.ColumnOrder,
                    Name = "Test"
                },
                ImportTemplateItems = new ImportTemplateItems
                {
                    Item = new[]
                    {
                        new Item {ColumnNumber = 0, AtomTable = "Invalid"},
                    }.ToList()
                }
            };
            var ex = Assert.Throws<InvalidOperationException>(() => template.CreateMap(loggerFactory.CreateLogger<DynamicImportMap<Model>>()));
            Assert.Equal("\"Invalid\" is not supported for import", ex.Message);
        }

        [Theory]
        [InlineAutoData("Model", Delimiter.Comma)]
        [InlineAutoData("Portfolio", Delimiter.Pipe)]
        [InlineAutoData("TaxLot", Delimiter.Tab)]
        [InlineAutoData("Security", Delimiter.Unknown)]
        [InlineAutoData("Order", Delimiter.Unknown)]
        public void CreateConfiguration(string typeName, Delimiter delimiter, bool hasHeader)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var template = new Template
            {
                Detail = new Detail
                {
                    Delimiter = delimiter != Delimiter.Unknown ? delimiter : Delimiter.Tab,
                    Description = "Test",
                    HasHeaders = hasHeader,
                    MappingType = MappingType.ColumnOrder,
                    Name = "Test"
                },
                ImportTemplateItems = new ImportTemplateItems
                {
                    Item = new[]
                    {
                        new Item {ColumnNumber = 1, AtomField = "Id", AtomTable = typeName, ImportField = "Test Id" }
                    }.ToList()
                }
            };
            var config = template.CreateConfiguration(loggerFactory.CreateLogger<DynamicImportMap<Model>>());
            Assert.IsType<CsvConfiguration>(config);
            Assert.True(config.HasHeaderRecord);
            Assert.Equal(template.Detail.Delimiter switch
            {
                Delimiter.Comma => ",",
                Delimiter.Tab => "\t",
                Delimiter.Pipe => "|",
                Delimiter.Unknown => "\0",
                _ => throw new NotImplementedException()
            }, config.Delimiter);
        }
    }
}