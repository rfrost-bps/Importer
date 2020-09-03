using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CsvHelper;
using Importer.Helpers;
using Importer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("Importer.Test")]
namespace Importer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ILogger _logger;

        public ImportController(ILogger<ImportController> logger)
        {
            _logger = logger;
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
            var config = template.CreateConfiguration(_logger);
            var importSection = await reader.ReadNextSectionAsync();
            using var stream = new StreamReader(importSection.Body);
            using var csv = new CsvReader(stream, config);
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
