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
            var marker = ThisMethodDoesNotExistAndWillNotCompile();

            return Ok(new
            {
                message = "IF YOU SEE THIS THE CI GATE FAILED",
                marker,
                machine = Environment.MachineName,
                utcNow = DateTime.UtcNow
            });
        }
    }
}
