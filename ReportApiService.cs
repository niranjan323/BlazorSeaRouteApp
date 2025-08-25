using System.Net.Http.Json;
using SeaRouteModel; // Or your shared models namespace

public class ReportApiService
{
    private readonly HttpClient _httpClient;

    public ReportApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId)
        => await _httpClient.GetFromJsonAsync<RecordReductionFactorsResponse>($"api/v1/reports/record_reduction_factors/{recordId}");

    public async Task<RecordDetailsResponse?> GetRecordDetailsAsync(string recordId)
        => await _httpClient.GetFromJsonAsync<RecordDetailsResponse>($"api/v1/reports/records/{recordId}");

    public async Task<RouteVersionDetailsResponse?> GetRouteVersionDetailsAsync(string routeVersionId)
        => await _httpClient.GetFromJsonAsync<RouteVersionDetailsResponse>($"api/v1/reports/route_versions/{routeVersionId}");

    public async Task<ActiveVesselResponse?> GetActiveVesselAsync(string recordId)
        => await _httpClient.GetFromJsonAsync<ActiveVesselResponse>($"api/v1/reports/records/{recordId}/active_vessel");
}