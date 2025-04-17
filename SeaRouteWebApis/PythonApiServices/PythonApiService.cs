using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;

namespace SeaRouteWebApis.PythonApiServices
{
    public class PythonApiService : IPythonApiService
    {
        private readonly HttpClient _httpClient;

        public PythonApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CalculateRouteAsync(RouteRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("https://as-ngea-digitalrule-002.azurewebsites.net/calculate-route", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException($"Error calling Python API: {response.StatusCode}");
        }

    }
}
