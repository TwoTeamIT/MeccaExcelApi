using DucatiMeccaExcelApi.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DucatiMeccaExcelApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Test : ControllerBase
    {
        public readonly SecurityOptions _securityOptions;

        public Test(Microsoft.Extensions.Options.IOptions<SecurityOptions> securityOptions)
        {
            _securityOptions = securityOptions.Value;
        }

        [HttpGet("")]
        public IActionResult TestConnection()
        {
            var username = User.Identity?.Name;  // Restituirà DOMINIO\Username

            return Ok(new { Message = "Test endpoint is working for username " + 
                DomainUpnManager.GetUpnFromDomain(User.Identity?.Name ?? "anonymous", _securityOptions) });
        });
        }
    }
}
