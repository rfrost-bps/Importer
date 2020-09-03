using Microsoft.AspNetCore.Mvc;

namespace Importer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index() => Ok();
    }
}