using System.Diagnostics.CodeAnalysis;
using Importer.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class MultipartRequestHelperShould
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("multipart/", true)]
        [InlineData("....multipart/", true)]
        public void EvaluateIsMultiPartContentType(string contentType, bool result)
        {
            var moq = new Mock<HttpRequest>();
            moq.SetupGet(r => r.ContentType).Returns(contentType);
            Assert.Equal(result, moq.Object.IsMultipartContentType());
        }
    }
}