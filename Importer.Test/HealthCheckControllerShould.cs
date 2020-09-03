using System.Diagnostics.CodeAnalysis;
using Importer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Importer.Test
{
    [ExcludeFromCodeCoverage]
    public class HealthCheckControllerShould
    {
        [Fact]
        public void Exist()
        {
            Assert.IsAssignableFrom<ControllerBase>(new HealthCheckController());
        }

        [Fact]
        public void IndexGetReturnsOk()
        {
            var result = new HealthCheckController().Index();
            Assert.IsAssignableFrom<OkResult>(result);
        }
    }
}