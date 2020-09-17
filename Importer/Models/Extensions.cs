using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Blaze.Domain.Impl;
using Blaze.Domain.Interfaces;
using CsvHelper.Configuration;
using EventStore.ClientAPI;
using Importer.Helpers;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Importer.Models
{
    internal static class Extensions
    {
        public static string TableName(this Template template)
        {
            if (template.ImportTemplateItems.Item.Select(i => i.AtomTable).Distinct().Count() > 1)
                throw new InvalidOperationException("Import into multiple tables is not supported.");

            return template.ImportTemplateItems.Item.Select(i => i.AtomTable).FirstOrDefault();
        }

        public static ClassMap CreateMap(this Template template, ILogger logger)
        {
            var tableName = template.TableName();

            return tableName switch
            {
                "Model" => new DynamicImportMap<Model>(template, logger),
                "Portfolio" => new DynamicImportMap<Portfolio>(template, logger),
                "TaxLot" => new DynamicImportMap<TaxLot>(template, logger),
                "Security" => new DynamicImportMap<Security>(template, logger),
                "Order" => new DynamicImportMap<Order>(template, logger),
                _ => throw new InvalidOperationException($"\"{tableName}\" is not supported for import")
            };
        }

        public static CsvConfiguration CreateConfiguration(this Template template, ILogger logger)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            config.Delimiter = template.Detail.Delimiter switch
            {
                Delimiter.Comma => ",",
                Delimiter.Pipe => "|",
                Delimiter.Tab => "\t",
                Delimiter.Unknown => throw new ConfigurationException("The template has an unknown delimiter."),
                _ => config.Delimiter
            };
            config.HasHeaderRecord = template.Detail.HasHeaders;
            config.RegisterClassMap(template.CreateMap(logger));
            return config;
        }
    }
}