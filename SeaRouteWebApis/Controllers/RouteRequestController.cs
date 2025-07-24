using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;
using System.Text.Json;
using System.Collections.Generic;

namespace SeaRouteWebApis.Controllers;

[Route("api/v1/RouteRequest")]
[ApiController]





public class RouteRequestController : SeaRouteBaseController<RouteRequest>
{
    public RouteRequestController(ILoggerFactory loggerFactory, IRepository<RouteRequest> portRepository)
        : base(loggerFactory, portRepository)
    {
    }

    [Route("RouteRequest")]
    [HttpPost]
    public IActionResult GetPortList([FromBody] RouteRequest routeRequest)
    {
        if (routeRequest == null || routeRequest.Origin.Length != 2 || routeRequest.Destination.Length != 2)
        {
            return BadRequest("Invalid request");
        }

        // Simulate a route result like FastAPI + searoute would
        var route = new
        {
            type = "FeatureCollection",
            features = new[]
            {
                new {
                    type = "Feature",
                    geometry = new {
                        type = "LineString",
                        coordinates = new[]
                        {
                            routeRequest.Origin,
                            routeRequest.Destination
                        }
                    },
                    properties = new {
                        description = "Simulated sea route"
                    }
                }
            }
        };

        var routeLength = CalculateDistance(routeRequest.Origin, routeRequest.Destination, routeRequest.Units);
        var response = new
        {
            route = route,
            route_length = $"{routeLength:F1} {routeRequest.Units}",
            route_properties = new Dictionary<string, object>
            {
                { "length", routeLength },
                { "units", routeRequest.Units },
                { "ports_included", routeRequest.IncludePorts },
                { "only_terminals", routeRequest.OnlyTerminals }
            }
        };

        return Ok(response);
    }

    private double CalculateDistance(double[] origin, double[] destination, string units)
    {
        // Haversine formula for distance in kilometers
        double R = (units.ToLower() == "mi") ? 3958.8 : 6371.0;

        double lat1 = DegreesToRadians(origin[0]);
        double lon1 = DegreesToRadians(origin[1]);
        double lat2 = DegreesToRadians(destination[0]);
        double lon2 = DegreesToRadians(destination[1]);

        double dlat = lat2 - lat1;
        double dlon = lon2 - lon1;

        double a = Math.Pow(Math.Sin(dlat / 2), 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Pow(Math.Sin(dlon / 2), 2);

        double c = 2 * Math.Asin(Math.Sqrt(a));
        return R * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
