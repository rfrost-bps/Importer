using System;
using Microsoft.AspNetCore.Http;

namespace Importer.Helpers
{
    public static class MultipartRequestHelper
    {
        public static bool IsMultipartContentType(this HttpRequest request)
        {
            var contentType = request.ContentType;
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}