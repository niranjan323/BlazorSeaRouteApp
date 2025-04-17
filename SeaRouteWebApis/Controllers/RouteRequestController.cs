using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;

namespace SeaRouteWebApis.Controllers;

[Route("api/v1/RouteRequest")]
[ApiController]
public class RouteRequestController : SeaRouteBaseController<RouteRequest>
{
    SeaRouteWebApis.Interfaces.IPythonApiService _pythonApiService;
    public RouteRequestController(ILoggerFactory loggerFactory, IRepository<RouteRequest> portRepository, IPythonApiService pythonApiService)
             : base(loggerFactory, portRepository)
    {
        _pythonApiService = pythonApiService;
    }


    [Route("RouteRequest")]
    [HttpPost]
    public IActionResult GetPortList([FromBody] RouteRequest routeRequest)
    {
        var ports = _pythonApiService.CalculateRouteAsync(routeRequest);
        if (ports == null)
        {
            return NotFound();
        }
        return Ok(ports);
    }


}
