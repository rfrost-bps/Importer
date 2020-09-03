using System.Diagnostics.CodeAnalysis;
using System.IO;
using AutoFixture.Xunit2;
using Importer.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class MultipartRequestHelperShould
    {
        [Theory, AutoMoqData]
        public void GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = MultipartRequestHelper.GetBoundary(contentType, contentType.Boundary.Length);
            Assert.NotEmpty(boundary);
        }

        [Theory, AutoMoqData]
        public void ThrowIfGetBoundaryWhenBoundaryIsNullOrWhiteSpace(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            contentType.Boundary = null;
            var ex = Assert.Throws<InvalidDataException>(() =>
                MultipartRequestHelper.GetBoundary(contentType, lengthLimit));
            Assert.Equal("Missing content-type boundary.", ex.Message);
            contentType.Boundary = string.Empty;
            ex = Assert.Throws<InvalidDataException>(() =>
                MultipartRequestHelper.GetBoundary(contentType, lengthLimit));
            Assert.Equal("Missing content-type boundary.", ex.Message);
        }

        [Theory, AutoMoqData]
        public void ThrowIfGetBoundaryWhenBoundaryLengthTooLong(MediaTypeHeaderValue contentType)
        {
            var ex = Assert.Throws<InvalidDataException>(() =>
                MultipartRequestHelper.GetBoundary(contentType, 1));
            Assert.Equal("Multipart boundary length limit 1 exceeded.", ex.Message);
        }

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

        [Theory, AutoData]
        public void EvaluateHasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            Assert.False(MultipartRequestHelper.HasFormDataContentDisposition(null));
            Assert.False(MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition));
            contentDisposition.DispositionType = new StringSegment("form-data");
            Assert.False(MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition));
            contentDisposition.FileName = StringSegment.Empty;
            Assert.False(MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition));
            contentDisposition.FileNameStar = StringSegment.Empty;
            Assert.True(MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition));
        }

        [Theory]
        [InlineAutoData(null, null, false)]
        [InlineAutoData(null, "null", true)]
        [InlineAutoData("null", null, true)]
        [InlineAutoData("null", "null", true)]
        public void EvaluateHasFileContentDisposition(string fileName, string fileNameStar, bool result, ContentDispositionHeaderValue contentDisposition)
        {
            Assert.False(MultipartRequestHelper.HasFileContentDisposition(null));
            Assert.False(MultipartRequestHelper.HasFileContentDisposition(contentDisposition));
            contentDisposition.DispositionType = new StringSegment("form-data");
            Assert.True(MultipartRequestHelper.HasFileContentDisposition(contentDisposition));
            contentDisposition.FileName = fileName;
            contentDisposition.FileNameStar = fileNameStar;
            Assert.Equal(result, MultipartRequestHelper.HasFileContentDisposition(contentDisposition));
        }
    }
}