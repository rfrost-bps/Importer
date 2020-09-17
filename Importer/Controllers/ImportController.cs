using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Blaze.Domain.Impl;
using Blaze.Domain.Interfaces;
using CsvHelper;
using EventStore.ClientAPI;
using Importer.Helpers;
using Importer.Models;
using Importer.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

[assembly:InternalsVisibleTo("Importer.Test")]
namespace Importer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IRebalanceRepository _repository;

        public ImportController(ILogger<ImportController> logger, IRebalanceRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromHeader] string correlationKey)
        {
            if (string.IsNullOrEmpty(correlationKey)) throw new ArgumentNullException(nameof(correlationKey));
            if (!Request.IsMultipartContentType()) throw new InvalidOperationException($"Expected multipart content type but, content type was {Request.ContentType}");

            var reader = new MultipartReader("========", Request.Body);
            var section = await reader.ReadNextSectionAsync();
            var imports = new List<Task>();

            while (section != null)
            {
                imports.Add(ImportStream(section));
                section = await reader.ReadNextSectionAsync();
            }

            await Task.WhenAll(imports);
            return Ok();
        }

        internal async Task ImportStream(MultipartSection section)
        {
            var reader = new MultipartReader("++++++++", section.Body);
            var templateSection = await reader.ReadNextSectionAsync();
            var template = await ReadTemplate(templateSection);
            var recordType = Assembly.GetAssembly(typeof(IBlazeEntity))?.ExportedTypes.FirstOrDefault(t => t.Name.Equals(template.TableName()));

            if (recordType == null)
                throw new InvalidOperationException($"Cannot import records into object of type {template.TableName()}.");

            var importSection = await reader.ReadNextSectionAsync();
            using var stream = new StreamReader(importSection.Body);
            var config = template.CreateConfiguration(_logger);
            using var rdr = new CsvReader(stream, config);

            var records = rdr.GetRecords(recordType);

            switch (recordType.Name)
            {
                case "Portfolio":
                    await _repository.SaveAsync(records.OfType<Portfolio>().ToList(), default, From);
                    break;

                case "TaxLot":
                    await _repository.SaveAsync(records.OfType<TaxLot>().ToList(), default, From);
                    break;

                case "Security":
                    await _repository.SaveAsync(records.OfType<Security>().ToList(), default, From);
                    break;

                case "Order":
                    await _repository.SaveAsync(records.OfType<Order>().ToList(), default, From);
                    break;

                case "Model":
                    await _repository.SaveAsync(records.OfType<Model>().ToList(), default, From);
                    break;

                case "Allocation":
                    await _repository.SaveAsync(records.OfType<Allocation>().ToList(), default, From);
                    break;
            }
        }

        internal static EventData From<TEntity>(TEntity entity) where TEntity : IBlazeEntity
        { 
            entity.Id = Guid.NewGuid();
            return new EventData(
                entity.Id,
                typeof(TEntity).Name,
                true,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity)),
                null);
        }

        internal static readonly XmlSerializer TemplateSerializer = new XmlSerializerFactory().CreateSerializer(typeof(Template));

        internal static async Task<Template> ReadTemplate(MultipartSection section)
        {
            var content = new StreamContent(section.Body);
            var text = await content.ReadAsStringAsync();
            return TemplateSerializer.Deserialize(new StringReader(text)) as Template;
        }
    }
}
