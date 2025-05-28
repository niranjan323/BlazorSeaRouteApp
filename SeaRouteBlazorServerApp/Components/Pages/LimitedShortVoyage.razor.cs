using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SeaRouteModel.Models;
using System.Text.Json;
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
        private List<PortModel> _ports = new List<PortModel>();
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

        // changes 
        private string GetDateInputStyle(DateOnly date)
        {
            if (date != default && date < DateOnly.FromDateTime(DateTime.Now))
            {
                return "background-color:#E5E5E5;";
            }
            return "";
        }

        private string GetMinArrivalDate()
        {
            // Arrival date should be at least the departure date or today, whichever is later
            var today = DateOnly.FromDateTime(DateTime.Now);
            var minDate = today;

            if (reductionFactor.DateOfDeparture != default && reductionFactor.DateOfDeparture > today)
            {
                minDate = reductionFactor.DateOfDeparture;
            }
            return minDate.ToString("yyyy-MM-dd");
        }

        private void OnDepartureDateChanged()
        {
            // If departure date is changed and arrival date is earlier than departure date,
            // reset arrival date
            if (reductionFactor.DateOfDeparture != default &&
                reductionFactor.DateOfArrival != default &&
                reductionFactor.DateOfArrival < reductionFactor.DateOfDeparture)
            {
                reductionFactor.DateOfArrival = default;
            }
            StateHasChanged();
        }
        public PortSelectionModel LimitedDeparturePortSelection { get; set; } = new();
        public PortSelectionModel LimitedArrivalPortSelection { get; set; } = new();
        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;

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
        //private async Task SearchReductionDepartureLocation()
        //{
        //    if (!string.IsNullOrWhiteSpace(reductionDepartureLocationQuery))
        //    {

        //        await JS.InvokeVoidAsync("searchLocation", reductionDepartureLocationQuery, true);
        //    }
        //}
        private async Task SearchReductionDepartureLocation()
        {
            if (!string.IsNullOrWhiteSpace(reductionDepartureLocationQuery))
            {
                var searchResults = await SearchPortsAsync(reductionDepartureLocationQuery);

                var tempPortSelection = new PortSelectionModel
                {
                    SearchTerm = reductionDepartureLocationQuery,
                    SearchResults = searchResults
                };

                if (searchResults.Any())
                {
                    LimitedDeparturePortSelection = tempPortSelection;
                }
            }
        }
        private async Task UpdateReductionDeparturePortSearch(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            reductionDepartureLocationQuery = newPort.Name;

            // Save to your main model
            reductionFactor.PortOfDeparture = newPort.Name;

            if (!string.IsNullOrWhiteSpace(newPort.Name))
                await JS.InvokeVoidAsync("searchLocation", newPort.Name, true);

            portSelection.SearchResults.Clear();
            if (ValidateLimitedRouteData())
            {
                await CalculateLimitedRoute();
            }
            StateHasChanged();
        }
        public async Task<List<PortModel>> SearchPortsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return new List<PortModel>();
            }

            try
            {
                var response = await Http.GetAsync($"api/v1/portsapi/search?searchTerm={Uri.EscapeDataString(searchTerm)}");

                if (response.IsSuccessStatusCode)
                {
                    var cPortResults = await response.Content.ReadFromJsonAsync<List<CPorts>>() ?? new List<CPorts>();
                    var results = MapToPortModels(cPortResults);
                    // Update the local cache of ports for filtering
                    foreach (var port in results)
                    {
                        if (!_ports.Any(p => p.Unlocode == port.Unlocode))
                        {
                            _ports.Add(port);
                        }
                    }

                    return results;
                }

                return new List<PortModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching ports: {ex.Message}");
                return new List<PortModel>();
            }
        }
        // for mapping
        public static List<PortModel> MapToPortModels(List<CPorts> cPortsList)
        {
            return cPortsList?.Select(cport => PortExtensions.ToPortModel(cport)).ToList() ?? new List<PortModel>();
        }

        // nested static class to hold the method
        private static class PortExtensions
        {
            public static PortModel ToPortModel(CPorts cPort)
            {
                var (lat, lon) = GetCoordinates(cPort.CountryCode, cPort.PortName);

                return new PortModel
                {
                    Port_Id = cPort.PointId.ToString(),
                    Name = cPort.PortName,
                    Country_Code = cPort.CountryCode,
                    Country = GetCountryName(cPort.CountryCode),
                    Unlocode = cPort.Unlocode,
                    Port_Authority = cPort.PortAuthority,
                    Latitude = lat,
                    Longitude = lon,
                    Last_Updated = cPort.CreatedDate
                };
            }

            private static string GetCountryName(string countryCode)
            {
                return countryCode switch
                {
                    "SG" => "Singapore",
                    "NL" => "Netherlands",
                    "CN" => "China",
                    "US" => "United States",
                    "DE" => "Germany",
                    "KR" => "South Korea",
                    "FR" => "France",
                    "AU" => "Australia",
                    "IN" => "India",
                    "AE" => "United Arab Emirates",
                    _ => countryCode
                };
            }

            private static (double Latitude, double Longitude) GetCoordinates(string countryCode, string portName)
            {
                return (countryCode, portName.ToLower()) switch
                {
                    ("SG", "singapore") => (1.2644, 103.8200),         // Port of Singapore
                    ("NL", "rotterdam") => (51.9555, 4.1338),          // Port of Rotterdam
                    ("CN", "shanghai") => (31.2304, 121.4910),         // Port of Shanghai
                    ("US", "los angeles") => (33.7405, -118.2760),     // Port of Los Angeles
                    ("DE", "hamburg") => (53.5461, 9.9661),            // Port of Hamburg
                    ("KR", "busan") => (35.0951, 129.0403),            // Port of Busan
                    ("FR", "marseille") => (43.3522, 5.3396),          // Port of Marseille
                    ("AU", "sydney") => (-33.8593, 151.2046),          // Port of Sydney
                    ("IN", "mumbai") => (18.9536, 72.8358),            // Port of Mumbai
                    ("AE", "dubai") => (25.2711, 55.3051),             // Port of Dubai (Jebel Ali)
                    _ => (0.0, 0.0)
                };
            }

        }

        //private async Task SearchReductionArrivalLocation()
        //{
        //    if (!string.IsNullOrWhiteSpace(reductionArrivalLocationQuery))
        //    {
        //        await JS.InvokeVoidAsync("searchLocation", reductionArrivalLocationQuery, false);
        //    }
        //}
        private async Task SearchReductionArrivalLocation()
        {
            if (!string.IsNullOrWhiteSpace(reductionArrivalLocationQuery))
            {
                var searchResults = await SearchPortsAsync(reductionArrivalLocationQuery);

                var tempPortSelection = new PortSelectionModel
                {
                    SearchTerm = reductionArrivalLocationQuery,
                    SearchResults = searchResults
                };

                if (searchResults.Any())
                {
                    LimitedArrivalPortSelection = tempPortSelection;
                }
            }
        }
        private async Task UpdateReductionArrivalPortSearch(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            reductionArrivalLocationQuery = newPort.Name;

            // Save to model
            reductionFactor.PortOfArrival = newPort.Name;

            if (!string.IsNullOrWhiteSpace(newPort.Name))
                await JS.InvokeVoidAsync("searchLocation", newPort.Name, false);

            portSelection.SearchResults.Clear();
            if (ValidateLimitedRouteData())
            {
                await CalculateLimitedRoute();
            }
            StateHasChanged();
        }

        //private async Task HandleReductionDepartureEnterKey(KeyboardEventArgs e)
        //{
        //    if (e.Key == "Enter")
        //    {
        //        //this.reductionFactor.PortOfDeparture = this.reductionDepartureLocationQuery;
        //        await SearchReductionDepartureLocation();
        //    }
        //}

        private void HandleReductionDepartureEnterKey(ChangeEventArgs e)
        {
            reductionDepartureLocationQuery = e.Value?.ToString();

            _debounceService.Debounce(async () =>
            {
                await InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(reductionDepartureLocationQuery))
                    {
                        await SearchReductionDepartureLocation();
                        StateHasChanged();
                    }
                    else
                    {
                        if (LimitedDeparturePortSelection != null)
                        {
                            LimitedDeparturePortSelection.SearchResults?.Clear();
                            StateHasChanged();
                        }
                    }
                });
            });
        }
        private void HandleReductionArrivalEnterKey(ChangeEventArgs e)
        {
            reductionArrivalLocationQuery = e.Value?.ToString();

            _debounceService.Debounce(async () =>
            {
                await InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(reductionArrivalLocationQuery))
                    {
                        await SearchReductionArrivalLocation();
                        StateHasChanged();
                    }
                    else
                    {
                        if (LimitedArrivalPortSelection != null)
                        {
                            LimitedArrivalPortSelection.SearchResults?.Clear();
                            StateHasChanged();
                        }
                    }
                });
            });
        }
        //private async Task HandleReductionArrivalEnterKey(KeyboardEventArgs e)
        //{
        //    if (e.Key == "Enter")
        //    {
        //       // this.reductionFactor.PortOfArrival = this.reductionArrivalLocationQuery;
        //        await SearchReductionArrivalLocation();
        //    }
        //}

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
                var response = await Http.PostAsJsonAsync("api/v1/short_voyage/short_voyage_reduction_factor", reductionFactor);


                if (response.IsSuccessStatusCode)
                {
                    showResults = true;
                    StateHasChanged();
                    var result = await response.Content.ReadFromJsonAsync<ReductionFactor>();

                    if (result != null)
                    {
                        reductionFactor = result;
                        await SaveShortVoyage(reductionFactor);
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
        protected async Task SaveShortVoyage(ReductionFactor reductionFactor)
        {
            //await CalculateLimitedRoute();
            ShortVoyageRecord shortVoyage = new ShortVoyageRecord()
            {
                UserId = "1",
                RecordId = "1",
                DepartureTime = reductionFactor.DateOfDeparture.ToDateTime(reductionFactor.ETD),
                ArrivalTime = reductionFactor.DateOfArrival.ToDateTime(reductionFactor.ETA),
                ForecastTime = reductionFactor.WeatherForecastDate.ToDateTime(reductionFactor.WeatherForecasetTime),
                ForecastHswell = (double?)reductionFactor.WaveHeightHswell,
                ForecastHwind = (double?)reductionFactor.WaveHeightHwind,
                ReductionFactor = (double?)reductionFactor.ShortVoyageReductionFactor,
                ModifiedBy = "System",
                ModifiedDate = new DateTime()

            };
            // Call the API
            // using var httpClient = new HttpClient();
            // httpClient.BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/");
            // httpClient.BaseAddress = new Uri("https://localhost:7155/");

            var response = await Http.PostAsJsonAsync("api/v1/short_voyage", shortVoyage);
            if (response.IsSuccessStatusCode)
            {

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
            if (OnReportDataReady.HasDelegate)
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

        // pythone
        private bool ValidateLimitedRouteData()
        {
            return LimitedDeparturePortSelection?.Port != null &&
                   LimitedArrivalPortSelection?.Port != null;
        }
        private SeaRouteModel.Models.RouteRequest PrepareLimitedRouteRequest()
        {
            return new SeaRouteModel.Models.RouteRequest
            {
                Origin = new double[] {
                    LimitedDeparturePortSelection.Port.Longitude,
            LimitedDeparturePortSelection.Port.Latitude
        },
                Destination = new double[] {
            LimitedArrivalPortSelection.Port.Longitude,
            LimitedArrivalPortSelection.Port.Latitude
        },
                Restrictions = new string[] { "northwest" }, // Customize if needed
                IncludePorts = true,
                Units = "km",
                OnlyTerminals = false
            };
        }
        private async Task CalculateLimitedRoute()
        {
            try
            {
                if (!ValidateLimitedRouteData())
                {
                    Console.WriteLine("Please complete all required limited route information");
                    return;
                }

                showResultsForReductionFactor = true;
                var routeRequest = PrepareLimitedRouteRequest();

                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/")
                };

                var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", routeRequest);
                await ProcessRouteCalculationResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating limited route: {ex.Message}");
            }
        }
        private async Task ProcessRouteCalculationResult(HttpResponseMessage response)
        {
            try
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                // Check if the route object exists
                if (root.TryGetProperty("route", out var routeElement))
                {
                    // Convert to raw JSON to pass to JS
                    var routeJson = routeElement.GetRawText();

                    // Pass the route GeoJSON directly to JavaScript
                    await JS.InvokeVoidAsync("createSeaRoutefromAPI", routeJson);

                    // Log route information
                    if (root.TryGetProperty("route_length", out var lengthElement))
                    {
                        Console.WriteLine($"Route length: {lengthElement.GetString()}");
                    }

                    if (root.TryGetProperty("route_properties", out var propertiesElement))
                    {
                        if (propertiesElement.TryGetProperty("duration_hours", out var durationElement))
                        {
                            Console.WriteLine($"Duration: {durationElement.GetDouble()} hours");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Route element not found in API response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing route result: {ex.Message}");
            }
        }
    }
}

