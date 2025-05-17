namespace SeaRouteBlazorServerApp.Components.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RouteListModel>> GetRouteList()
    {
        // For now, return sample data until the API is ready
        var sampleRoutes = new List<RouteListModel>
        {
            new RouteListModel
            {
                RecordId = Guid.NewGuid().ToString(),
                RecordName = "Marseille to Shanghai Route",
                DeparturePort = "Marseille",
                ArrivalPort = "Shanghai",
                VoyageDate = DateTime.Now.AddDays(30),
                ReductionFactor = 0.82,
                RouteDistance = 8562.5,
                VesselIMO = "9876543",
                VesselName = "Pacific Pioneer"
            },
            new RouteListModel
            {
                RecordId = Guid.NewGuid().ToString(),
                RecordName = "Rotterdam to Mumbai Route",
                DeparturePort = "Rotterdam",
                ArrivalPort = "Mumbai",
                VoyageDate = DateTime.Now.AddDays(45),
                ReductionFactor = 0.78,
                RouteDistance = 6789.4,
                VesselIMO = "8765432",
                VesselName = "Atlantic Voyager"
            }
        };

        return sampleRoutes;
    }

    public async Task<List<RouteListLegModel>> GetRouteLegsList(string routeId)
    {
        // Sample leg data
        return new List<RouteListLegModel>
        {
            new RouteListLegModel
            {
                RecordLegName = "Leg 1 - Direct Route",
                DeparturePort = "Singapore",
                ArrivalPort = "Shanghai",
                ReductionFactor = 0.85,
                Distance = 4500.3
            }
        };
    }
}