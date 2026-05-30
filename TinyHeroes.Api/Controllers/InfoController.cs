using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace TinyHeroes.Api.Controllers;

[Route("api/info")]
public class InfoController(IHostEnvironment env) : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        return Ok(new { version, environment = env.EnvironmentName });
    }
}
