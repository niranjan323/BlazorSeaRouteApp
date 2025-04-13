using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SeaRouteModel.Models;
using static System.Net.WebRequestMethods;

namespace SeaRouteBlazorServerApp.Components.Pages
{
    public partial class LimitedShortVoyage
    {
        [Parameter]
        public EventCallback OnBack { get; set; }
        [Parameter]
        public EventCallback OnShowReport { get; set; }
        [Parameter] 
        public EventCallback OnAddEditVessel { get; set; }

        [Parameter]
        public EventCallback<ReductionFactor> OnReportDataReady { get; set; }
        private int selectedTab = 1;
        private ElementReference reportMapContainer;
        private ReductionFactor reductionFactor = new ReductionFactor();
        private bool isLoading = false;
        private string errorMessage = string.Empty;
        private void CloseOverlay() { /* Logic to hide overlay */ }
        private decimal? routeReductionFactor = 0.82M;
        private int? routeDistance = 5952;
        private List<PortModel> _ports;
        private bool showReport = false;
        private bool showResults = false;
        private bool showResultsForReductionFactor = false;
        private bool showReportForReductionFactor = false;
        private DotNetObjectReference<LimitedShortVoyage>? objRef;
        private string LocationQuery { get; set; } = "";
        private string departureLocationQuery { get; set; } = "";
        private string arrivalLocationQuery { get; set; } = "";
        private string reductionDepartureLocationQuery = "";
        private string reductionArrivalLocationQuery = "";
        private string durationValidationMessage = "";
        private string weatherValidationMessage = "";
        private string durationSuggestion = "";
        private string weatherSuggestion = "";
        private bool isRouteSaved = false;
        private bool AddEditVessalReport = false;
        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;
            await GetSampleports();

        }
 
        private async Task GoBack()
        {
            showReport = false;
            showReportForReductionFactor = false;
            showResultsForReductionFactor = false;
            reductionFactor = new ReductionFactor();
            departureSearchResults = new List<PortModel>();
            arrivalSearchResults = new List<PortModel>();
            await JS.InvokeVoidAsync("resetMap");
            if (OnBack.HasDelegate)
            {
                await OnBack.InvokeAsync();
            }
        }

        private async Task CaptureMap()
        {
            bool success = await JS.InvokeAsync<bool>(
                "captureMapWithRoutes",
                "map",
                "reportMapContainer"
            );

            if (!success)
            {
                // Handle error (optional)
                Console.WriteLine("Failed to capture map");
            }
        }

        // Method to calculate voyage duration
        private void CalculateVoyageDuration()
        {
            durationValidationMessage = "";

            // Validate required fields
            if (reductionFactor.DateOfDeparture == default ||
                reductionFactor.ETD == default ||
                reductionFactor.DateOfArrival == default ||
                reductionFactor.ETA == default)
            {
                durationValidationMessage = "Please fill all departure and arrival fields";
                return;
            }

            try
            {
                var departureDateTime = reductionFactor.DateOfDeparture.ToDateTime(reductionFactor.ETD);
                var arrivalDateTime = reductionFactor.DateOfArrival.ToDateTime(reductionFactor.ETA);

                // Calculate duration
                var timeSpan = arrivalDateTime - departureDateTime;
                reductionFactor.Duration = Convert.ToInt32(timeSpan.TotalHours);

                // Set status
                reductionFactor.DurationOk = (reductionFactor.Duration > 0 && reductionFactor.Duration <= 72) ? "OK" : "NA";

                // Validate positive duration
                if (reductionFactor.Duration <= 0)
                {
                    durationValidationMessage = "Arrival must be after departure";
                }
                if (reductionFactor.Duration > 72)
                {
                    durationValidationMessage = "Voyage duration exceeds 72 hours. Short voyage reduction factor not applicable.";
                }
            }
            catch (Exception ex)
            {
                durationValidationMessage = $"Error calculating duration: {ex.Message}";
            }
        }
        private void ShowDurationSuggestions()
        {
            durationSuggestion = "• Voyage duration is calculated from departure to arrival\n" +
                               "• Short voyages must be ≤72 hours\n" +
                               "• Arrival must be after departure time";
        }
        private void HideDurationSuggestions()
        {
            durationSuggestion = "";
        }
        private void ShowWeatherSuggestions()
        {
            weatherSuggestion = "• Forecast must be before departure time\n" +
                              "• Valid forecast window: 1-6 hours before ETD\n" +
                              "• Enter both date and time for accurate calculation";
        }

        private void HideWeatherSuggestions()
        {
            weatherSuggestion = "";
        }
        // Method to calculate weather forecast
        private void CalculateWeatherForecast()
        {
            weatherValidationMessage = "";

            // Validate required fields
            if (reductionFactor.WeatherForecastDate == default ||
                reductionFactor.WeatherForecasetTime == default)
            {
                weatherValidationMessage = "Please fill both forecast date and time";
                return;
            }

            try
            {
                var departureDateTime = reductionFactor.DateOfDeparture.ToDateTime(reductionFactor.ETD);
                var forecastDateTime = reductionFactor.WeatherForecastDate.ToDateTime(reductionFactor.WeatherForecasetTime);

                // Calculate forecast hours before ETD
                var timeSpan = departureDateTime - forecastDateTime;
                reductionFactor.WeatherForecastBeforeETD = Convert.ToInt32(timeSpan.TotalHours);

                // Set status
                reductionFactor.WeatherForecastBeforeETDOK = (reductionFactor.WeatherForecastBeforeETD > 0 &&
                                                             reductionFactor.WeatherForecastBeforeETD <= 6) ? "OK" : "NA";

                // Validate positive forecast time
                if (reductionFactor.WeatherForecastBeforeETD <= 0)
                {
                    weatherValidationMessage = "Forecast must be before departure time";
                }
                if (reductionFactor.WeatherForecastBeforeETD > 6)
                {
                    weatherValidationMessage = "Weather forecast must be within 6 hours of departure";
                }
            }
            catch (Exception ex)
            {
                weatherValidationMessage = $"Error calculating forecast: {ex.Message}";
            }
        }
        private bool ValidateReductionFactor()
        {
            List<string> validationErrors = new List<string>();

            // Check required vessel info
            // if (string.IsNullOrWhiteSpace(reductionFactor.VesselName))
            //     validationErrors.Add("Vessel Name is required");

            // if (string.IsNullOrWhiteSpace(reductionFactor.IMONo))
            //     validationErrors.Add("Vessel IMO Number is required");

            // if (reductionFactor.Breadth <= 0)
            //     validationErrors.Add("Vessel Breadth must be greater than 0");

            // Check port information
            if (string.IsNullOrWhiteSpace(reductionFactor.PortOfDeparture))
                validationErrors.Add("Port of Departure is required");

            if (string.IsNullOrWhiteSpace(reductionFactor.PortOfArrival))
                validationErrors.Add("Port of Arrival is required");

            // Check dates and times
            DateTime departureDateTime = reductionFactor.DateOfDeparture.ToDateTime(reductionFactor.ETD);
            DateTime arrivalDateTime = reductionFactor.DateOfArrival.ToDateTime(reductionFactor.ETA);

            if (departureDateTime >= arrivalDateTime)
                validationErrors.Add("Arrival date/time must be after departure date/time");

            // Validate weather forecast
            DateTime forecastDateTime = reductionFactor.WeatherForecastDate.ToDateTime(reductionFactor.WeatherForecasetTime);

            if (forecastDateTime >= departureDateTime)
                validationErrors.Add("Weather forecast date/time must be before departure date/time");

            // Calculate time difference between forecast and departure
            TimeSpan forecastDifference = departureDateTime - forecastDateTime;
            if (forecastDifference.TotalHours > 6)
                validationErrors.Add("Weather forecast must be within 6 hours of departure");

            if (forecastDifference.TotalHours <= 0)
                validationErrors.Add("Weather forecast must be before departure time");

            // Validate wave height parameters
            if (reductionFactor.WaveHeightHswell <= 0)
                validationErrors.Add("Wave Height Hswell must be greater than 0");

            if (reductionFactor.WaveHeightHwind <= 0)
                validationErrors.Add("Wave Height Hwind must be greater than 0");

            // Check voyage duration constraints
            TimeSpan voyageDuration = arrivalDateTime - departureDateTime;
            if (voyageDuration.TotalHours <= 0 || voyageDuration.TotalHours > 72)
                validationErrors.Add("Voyage duration must be between 0 and 72 hours");

            // If there are any validation errors, display them and return false
            if (validationErrors.Count > 0)
            {
                errorMessage = $"Please correct the following errors:\n{string.Join("\n", validationErrors)}";
                return false;
            }
            if (reductionFactor.DurationOk == "NA")
            {
                errorMessage = "Voyage duration exceeds 72 hours. Short voyage reduction factor not applicable.";
                return false;
            }
            return true;
        }
        private async Task SearchReductionDepartureLocation()
        {
            if (!string.IsNullOrWhiteSpace(reductionDepartureLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", reductionDepartureLocationQuery, true);
            }
        }

        private async Task SearchReductionArrivalLocation()
        {
            if (!string.IsNullOrWhiteSpace(reductionArrivalLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", reductionArrivalLocationQuery, false);
            }
        }

        private async Task HandleReductionDepartureEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                this.reductionFactor.PortOfDeparture = this.reductionDepartureLocationQuery;
                await SearchReductionDepartureLocation();
            }
        }

        private async Task HandleReductionArrivalEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                this.reductionFactor.PortOfArrival = this.reductionArrivalLocationQuery;
                await SearchReductionArrivalLocation();
            }
        }

        // separate
        private async Task SearchDepartureLocation()
        {
            if (!string.IsNullOrWhiteSpace(departureLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", departureLocationQuery, true);
            }
        }

        private async Task SearchArrivalLocation()
        {
            if (!string.IsNullOrWhiteSpace(arrivalLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", arrivalLocationQuery, false);
            }
        }

        private async Task HandleDepartureEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {

                await SearchDepartureLocation();
            }
        }

        private async Task HandleArrivalEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await SearchArrivalLocation();
            }
        }
     

        private string departureSearchTerm = string.Empty;
        private string arrivalSearchTerm = string.Empty;
        private List<PortModel> departureSearchResults = new List<PortModel>();
        private List<PortModel> arrivalSearchResults = new List<PortModel>();

      
        private async Task CalculateReductionFactor()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;
                isLoading = true;
                if (!ValidateReductionFactor())
                {
                    isLoading = false;
                    return;
                }

                // Call the API
                var response = await Http.PostAsJsonAsync("api/searoute/short_voyage_reduction_factor", reductionFactor);


                if (response.IsSuccessStatusCode)
                {
                    showResults = true;
                    StateHasChanged();
                    var result = await response.Content.ReadFromJsonAsync<ReductionFactor>();

                    if (result != null)
                    {
                        reductionFactor = result;

                        await Task.Delay(500);
                        StateHasChanged();
                        await InitializeChart();

                    }
                }
                else
                {
                    errorMessage = $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                    Logger.LogError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Exception: {ex.Message}";
                Logger.LogError(ex, "Error calculating reduction factor");
            }
            finally
            {
                isLoading = false;
            }
        }
        private async Task InitializeChart()
        {

            try
            {

                var chartData = new
                {
                    commonX = reductionFactor.CommonX.ToString(),
                    commonY = reductionFactor.CommonY.ToString()
                };

                // Serialize to JSON
                var jsonConfig = System.Text.Json.JsonSerializer.Serialize(chartData);
                Console.WriteLine($"Sending chart data: {jsonConfig}");

                // Call JavaScript function
                await JS.InvokeVoidAsync("drawChart", "reductionChart1", jsonConfig);

            }
            catch (Exception ex)
            {
                errorMessage = $"Error initializing report chart: {ex.Message}";
            }
        }

        private async Task InitializeChartForReport()
        {
            try
            {

                var chartData = new
                {
                    commonX = reductionFactor.CommonX.ToString(),
                    commonY = reductionFactor.CommonY.ToString()
                };

                // Serialize to JSON
                var jsonConfig = System.Text.Json.JsonSerializer.Serialize(chartData);
                Console.WriteLine($"Sending chart data: {jsonConfig}");

                // Call JavaScript function
                await JS.InvokeVoidAsync("drawChart2", "reductionChart2", jsonConfig);

            }
            catch (Exception ex)
            {
                errorMessage = $"Error initializing report chart: {ex.Message}";
            }
        }

        private async Task ShowReport()
        {

            if (OnShowReport.HasDelegate)
            {
                await OnShowReport.InvokeAsync();
            }
           if(OnReportDataReady.HasDelegate)
            {
                await OnReportDataReady.InvokeAsync(reductionFactor);
            }
            await Task.Delay(200);
            await InitializeChartForReport();
            await Task.Delay(100);
            await CaptureMap();
        }

        private async Task ShowReportForReductionFactor()
        {

            showReportForReductionFactor = true;
            await Task.Delay(100);
            await CaptureMap();
        }

        private async Task RearranegeReport()
        {
            await Task.Delay(100);
            await InitializeChartForReport();
            await Task.Delay(100);
            await CaptureMap();
        }

        private void CloseReport()
        {
            showReport = false;
        }
        private void CloseReportForReductionFactor()
        {
            showReportForReductionFactor = false;
        }

        private void CloseResults()
        {
            showResults = false;
        }


        private async Task AddEditVesselInfo()
        {
            if (OnAddEditVessel.HasDelegate)
            {
                await OnAddEditVessel.InvokeAsync();
            }
        }

        private async Task SaveRoute()
        {
            isRouteSaved = true; // Show the message
            StateHasChanged();   // Update the UI

            await Task.Delay(3000); // Wait for 3 seconds

            isRouteSaved = false; // Hide the message
            StateHasChanged();
        }
        private void SearchDeparturePorts()
        {
            departureSearchResults = SearchPorts(departureSearchTerm);
        }

        private void SearchArrivalPorts()
        {
            arrivalSearchResults = SearchPorts(arrivalSearchTerm);
        }




        private async Task HandleDepartureEnterKey(KeyboardEventArgs e, PortSelectionModel portSelection)
        {
            if (e.Key == "Enter")
            {
                await SearchDeparturePortsForExisting(portSelection);
            }
        }

        private async Task HandleArrivalEnterKey(KeyboardEventArgs e, PortSelectionModel portSelection)
        {
            if (e.Key == "Enter")
            {
                await SearchArrivalPortsForExisting(portSelection);
            }
        }

        private async Task SearchDeparturePortsForExisting(PortSelectionModel portSelection)
        {
            if (portSelection?.SearchTerm == null || _ports == null)
            {
                return;
            }

            portSelection.SearchResults = SearchPorts(portSelection.SearchTerm);

            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
            {
                await JS.InvokeVoidAsync("zoomAndPinLocation", portSelection.SearchTerm, true);
            }
        }

        private async Task SearchArrivalPortsForExisting(PortSelectionModel portSelection)
        {
            if (portSelection?.SearchTerm == null || _ports == null)
            {
                return;
            }

            portSelection.SearchResults = SearchPorts(portSelection.SearchTerm);

            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
            {
                await JS.InvokeVoidAsync("zoomAndPinLocation", portSelection.SearchTerm, false);
            }
        }







  
        private async Task EnableWaypointSelection()
        {
            if (JS is not null)
            {
                await JS.InvokeVoidAsync("setWaypointSelection", true);
            }
        }

        private void CalculateRouteReductionFactor()
        {
            // For demo purposes, just showing the results
            showResultsForReductionFactor = true;

            // In a real implementation, you would calculate the reduction factor here
            routeReductionFactor = 0.82M;
            routeDistance = 5952;
        }

        protected async Task GetSampleports()
        {
            _ports = new List<PortModel>
{
new PortModel
{
Legacy_Place_Id = 5000,
Port_Id = "POR-03000",
Name = "Marseille",
Country = "France",
Country_Code = "FRA",
Unlocode = "FRMRS",
Latitude = 43.2965,
Longitude = 5.3698
},
new PortModel
{
Legacy_Place_Id = 5001,
Port_Id = "POR-03001",
Name = "Singapore",
Country = "Singapore",
Country_Code = "SGP",
Unlocode = "SGSIN",
Latitude = 1.3521,
Longitude = 103.8198
},
// Add more sample ports as needed
};

        }


        public List<PortModel> SearchPorts(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm) || _ports == null)
            {
                departureSearchResults.Clear();
                return [];
            }
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<PortModel>();

            return _ports.Where(p =>
            p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Country.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Unlocode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

    }
}

