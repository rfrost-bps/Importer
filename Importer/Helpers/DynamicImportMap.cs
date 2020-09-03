using System.Linq;
using System.Linq.Expressions;
using CsvHelper.Configuration;
using Importer.Models;
using Microsoft.Extensions.Logging;

namespace Importer.Helpers
{
    public class DynamicImportMap<T> : ClassMap<T> where T : class, new()
    {
        public DynamicImportMap(Template importTemplate, ILogger logger)
        {
            var typeExpression = Expression.Parameter(typeof(T), "m");
            importTemplate
                .ImportTemplateItems
                .Item
                .ForEach(i =>
                {
                    if (!typeof(T).GetProperties().Any(p => p.Name.Equals(i.AtomField)))
                    {
                        logger.LogWarning($"Item for column no {i.ColumnNumber} references a field, \"{i.AtomField}\" that is not a property of {typeof(T).Name}");
                        return;
                    }
                    Map(typeof(T), typeof(T).GetMember(i.AtomField)[0]).Name(i.ImportField);
                });
        }
    }
}