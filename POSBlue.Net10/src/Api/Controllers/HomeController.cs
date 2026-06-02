using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Api.Controllers;

/// <summary>
/// Health-check endpoint (thay HomeController/Index trả View MVC cũ bằng JSON cho API thuần).
/// Giữ route gốc "" (root) và "Home/Index".
/// </summary>
[ApiController]
public class HomeController : ControllerBase
{
    [HttpGet("")]
    [HttpGet("Home/Index")]
    public IActionResult Index()
    {
        var version = VersionHelper.GetAssemblyModifiedDate();
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipServer = host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .LastOrDefault()?.ToString();

            return Ok(new HealthCheckDto { IPServer = ipServer, DbConnection = true, Version = version });
        }
        catch (Exception ex)
        {
            return Ok(new HealthCheckDto
            {
                IPServer = JsonConvert.SerializeObject(ex),
                DbConnection = false,
                Version = version,
            });
        }
    }
}
