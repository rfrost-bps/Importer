using System;
using System.Globalization;
using System.Linq;
using CsvHelper.Configuration;
using System.Xml.Serialization;
using Blaze.Domain.Impl;
using Importer.Helpers;
using Microsoft.Extensions.Logging;

namespace Importer.Models
{
    [XmlRoot(ElementName="Template")]
    public class Template {
        [XmlElement(ElementName="detail")]
        public Detail Detail { get; set; }
        [XmlElement(ElementName="ImportTemplateItems")]
        public ImportTemplateItems ImportTemplateItems { get; set; }

        public ClassMap CreateMap(ILogger logger)
        {
            if (ImportTemplateItems.Item.Select(i => i.AtomTable).Distinct().Count() > 1)
                throw new InvalidOperationException("Import into multiple tables is not supported.");

            var tableName = ImportTemplateItems.Item.Select(i => i.AtomTable).FirstOrDefault();

            return tableName switch
            {
                "Model" => new DynamicImportMap<Model>(this, logger),
                "Portfolio" => new DynamicImportMap<Portfolio>(this, logger),
                "TaxLot" => new DynamicImportMap<TaxLot>(this, logger),
                "Security" => new DynamicImportMap<Security>(this, logger),
                "Order" => new DynamicImportMap<Order>(this, logger),
                _ => throw new InvalidOperationException($"\"{tableName}\" is not supported for import")
            };
        }

        public CsvConfiguration CreateConfiguration(ILogger logger)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            config.Delimiter = Detail.Delimiter switch
            {
                Delimiter.Comma => ",",
                Delimiter.Pipe => "|",
                Delimiter.Tab => "\t",
                Delimiter.Unknown => throw new ConfigurationException("The template has an unknown delimiter."),
                _ => config.Delimiter
            };
            config.HasHeaderRecord = Detail.HasHeaders;
            config.RegisterClassMap(CreateMap(logger));
            return config;
        }
    }
}