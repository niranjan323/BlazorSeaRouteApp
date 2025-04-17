using SeaRouteModel.Models;

namespace SeaRouteWebApis.Interfaces
{
    public interface IPythonApiService
    {
        Task<string> CalculateRouteAsync(RouteRequest request);
    }
}
