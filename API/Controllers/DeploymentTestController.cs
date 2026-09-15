using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeploymentTestController : ControllerBase
    {
        [HttpGet("Ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "deployment pipeline works",
                machine = Environment.MachineName,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                utcNow = DateTime.UtcNow
            });
        }
    }
}
