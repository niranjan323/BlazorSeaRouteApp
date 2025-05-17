using SeaRouteModel.Models;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IApiService
{
    Task<List<RouteListModel>> GetRouteList();
    Task<List<RouteListLegModel>> GetRouteLegsList(string routeId);
}