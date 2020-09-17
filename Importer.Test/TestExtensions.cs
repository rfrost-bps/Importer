using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Blaze.Domain.Impl;
using Blaze.Domain.Interfaces;
using EventStore.ClientAPI;
using Importer.Controllers;
using Importer.Repository;
using Moq;
using Moq.Language.Flow;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public static class TestExtensions
    {
        public static void Setup(
            this Mock<IRebalanceRepository> moqRepo,
            Func<IList<IBlazeEntity>, Guid, Task<long>> valueFunction)
            => moqRepo
                .Setup(r => r.SaveAsync(
                    It.IsAny<IList<IBlazeEntity>>(),
                    It.IsAny<Guid>(),
                    ImportController.From))
                .Returns((IList<IBlazeEntity> l, Guid g, Func<IBlazeEntity, EventData> _) => valueFunction.Invoke(l,g));

        public static void Verify<TEntity>(
            this Mock<IRebalanceRepository> moqRepo, Times times = default) where TEntity : IBlazeEntity
            => moqRepo
                .Verify(r => r.SaveAsync(
                    It.IsAny<IList<TEntity>>(),
                    It.IsAny<Guid>(),
                    ImportController.From), times == default ? Times.AtLeastOnce() : times);

        public static MultipartContent AddContent(
            this MultipartContent modelContent,
            string aggregateType,
            StreamContent templateStream,
            StreamContent dataStream)
        {
            modelContent.Headers.Add("AggregateType", aggregateType);

            templateStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            templateStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("template") { Name = aggregateType };
            modelContent.Add(templateStream);

            dataStream.Headers.ContentType = MediaTypeHeaderValue.Parse("application/csv");
            dataStream.Headers.ContentDisposition = new ContentDispositionHeaderValue("data") { Name = "Model" };
            modelContent.Add(dataStream);
            return modelContent;
        }

        public static async Task<StreamContent> AsStreamContent(this string path)
            => new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(path))));
    }
}