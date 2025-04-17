using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SeaRouteModel.Models;
using static System.Net.WebRequestMethods;
using System.Net.Http;

namespace SeaRouteBlazorServerApp.Components.Pages
{
    public partial class ReductionFactorCal
    {
        [Parameter]
        public EventCallback OnBack { get; set; }
        [Parameter]
        public EventCallback OnAddEditVessel { get; set; }
        [Parameter]
        public EventCallback OnShowAbsReport { get; set; }
        [Parameter]
        public EventCallback OnShowReportForReductionFactor { get; set; }
        [Parameter]
        public EventCallback<(double, double)> OnCoordinatesCaptured { get; set; }
        private List<string> seasonalOptions = new() { "Annual", "Spring", "Summer", "Fall", "Winter" };
        private List<string> WaytypeOptions = new () { "ABS", "BMT" };
        private bool showDropdown = false;
        private bool showDropdownforwaypoint = false;
        private bool isValidExceedanceProbability = true;
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
        private DotNetObjectReference<ReductionFactorCal>? objRef;
        private string LocationQuery { get; set; } = "";
        private string departureLocationQuery { get; set; } = "";
        private string arrivalLocationQuery { get; set; } = "";
        private string reductionDepartureLocationQuery = "";
        private string reductionArrivalLocationQuery = "";
        private bool isRouteSaved = false;
        private bool AddEditVessalReport = false;

        private RouteModel routeModel = new RouteModel();
        private string departureSearchTerm = string.Empty;
        private string arrivalSearchTerm = string.Empty;
        private List<PortModel> departureSearchResults = new List<PortModel>();
        private List<PortModel> arrivalSearchResults = new List<PortModel>();
        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;
            await GetSampleports();

        }
        private void OnFocus()
        {
            showDropdown = true;
        }
        private void OnFocuswaypoint()
        {
            showDropdownforwaypoint = true;
        }
        private async void OnBlur()
        {
            await Task.Delay(150);
            await InvokeAsync(() =>
            {
                showDropdown = false;
                StateHasChanged();
            });

        }
        private async void OnBlurwaypoint()
        {
            await Task.Delay(150);
            await InvokeAsync(() =>
            {
                showDropdownforwaypoint = false;
                StateHasChanged();
            });
        }

        private void SelectOption(string option)
        {
            routeModel.SeasonalType = option;
            showDropdown = false;
        }
        private void SelectOptionForWayType(string option)
        {
            routeModel.WayType = option;
            showDropdownforwaypoint = false;
        }
        private void ValidateExceedanceProbability(ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString(), out double value))
            {
                isValidExceedanceProbability = value > 0 && value < 1;
                routeModel.ExceedanceProbability = value;
            }
            else
            {
                isValidExceedanceProbability = false;
            }
        }
        private void CloseVesselInfo()
        {
            AddEditVessalReport = false;
        }
        public void CaptureCoordinates(double latitude, double longitude)
        {
            if (routeModel.DepartureWaypoints.Count > 0)
            {
                var lastWaypoint = routeModel.DepartureWaypoints.Last();
                lastWaypoint.Latitude = latitude.ToString();
                lastWaypoint.Longitude = longitude.ToString();
                StateHasChanged();
            }
        }
        private async Task GoBack()
        {
            showReport = false;
            showReportForReductionFactor = false;
            showResultsForReductionFactor = false;
            reductionFactor = new ReductionFactor();
            routeModel = new RouteModel();
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


        //  ------------------------  ports  --------------------
        private async Task HandleDepartureEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {

                await SearchDepartureLocation();
            }
        }
        private async Task SearchDepartureLocation()
        {
            if (!string.IsNullOrWhiteSpace(departureLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", departureLocationQuery, true);
            }
        }
        private void AddDeparturePort()
        {
            routeModel.DeparturePorts.Add(new PortSelectionModel());
        }

        private void AddArrivalPort()
        {
            routeModel.ArrivalPorts.Add(new PortSelectionModel());
        }

        private void RemoveDeparturePort(PortSelectionModel port)
        {
            routeModel.DeparturePorts.Remove(port);
        }

        private void RemoveArrivalPort(PortSelectionModel port)
        {
            routeModel.ArrivalPorts.Remove(port);
        }

        private async Task HandleDepartureEnterKey(KeyboardEventArgs e, PortSelectionModel portSelection)
        {
            if (e.Key == "Enter")
            {
                await SearchDeparturePortsForExisting(portSelection);
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
        private void UpdateDeparturePort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            portSelection.SearchResults.Clear();
        }

        private async Task HandleArrivalEnterKey(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await SearchArrivalLocation();
            }
        }
        private async Task SearchArrivalLocation()
        {
            if (!string.IsNullOrWhiteSpace(arrivalLocationQuery))
            {
                await JS.InvokeVoidAsync("searchLocation", arrivalLocationQuery, false);
            }
        }
        private async Task HandleArrivalEnterKey(KeyboardEventArgs e, PortSelectionModel portSelection)
        {
            if (e.Key == "Enter")
            {
                await SearchArrivalPortsForExisting(portSelection);
            }
        }
        private void UpdateArrivalPort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            portSelection.SearchResults.Clear();
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
                    return await response.Content.ReadFromJsonAsync<List<PortModel>>() ?? new List<PortModel>();
                }

                //_logger.LogWarning($"Failed to search ports: {response.StatusCode}");
                return new List<PortModel>();
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error searching ports");
                return new List<PortModel>();
            }
        }
        //  ------------------------  end  --------------------


        private async Task CheckAndUpdateMap(WaypointModel waypoint)
        {
            if (!string.IsNullOrEmpty(waypoint.Latitude) && !string.IsNullOrEmpty(waypoint.Longitude))
            {
                if (double.TryParse(waypoint.Latitude, out double lat) && double.TryParse(waypoint.Longitude, out double lng))
                {
                    await JS.InvokeVoidAsync("updateMap", waypoint.Name, lat, lng);
                }
            }
        }

        private async Task ShowReportForReductionFactor()
        {

            if (OnShowReportForReductionFactor.HasDelegate)
            {
                await OnShowReportForReductionFactor.InvokeAsync();
            }
            await Task.Delay(100);
            await CaptureMap();
        }
        private async Task AddEditVesselInfo()
        {
            if (OnAddEditVessel.HasDelegate)
            {
                await OnAddEditVessel.InvokeAsync();
            }
        }
        private async Task ShowAbsReport()
        {
            if (OnShowAbsReport.HasDelegate)
            {
                await OnShowAbsReport.InvokeAsync();
            }
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



        private async Task DownloadReport()
        {
            // In a real app, this would trigger PDF generation and download
            await JS.InvokeVoidAsync("alert", "Download report functionality would be implemented here");
        }

        private async Task PrintReport()
        {
            await JS.InvokeVoidAsync("window.print");
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

        private void SelectDeparturePort(PortModel port)
        {
            var portSelection = new PortSelectionModel { Port = port };
            routeModel.DeparturePorts.Add(portSelection);
            departureSearchTerm = string.Empty;
            departureSearchResults.Clear();
        }

        private void SelectArrivalPort(PortModel port)
        {
            var portSelection = new PortSelectionModel { Port = port };
            routeModel.ArrivalPorts.Add(portSelection);
            arrivalSearchTerm = string.Empty;
            arrivalSearchResults.Clear();
        }

       
        private async Task AddDepartureWaypoint()
        {
            routeModel.DepartureWaypoints.Add(new WaypointModel());
            await EnableWaypointSelection();
        }
        private async Task EnableWaypointSelection()
        {
            if (JS is not null)
            {
                await JS.InvokeVoidAsync("setWaypointSelection", true);
            }
        }
        private async Task AddArrivalWaypoint()
        {
            routeModel.ArrivalWaypoints.Add(new WaypointModel());
            await EnableWaypointSelection();
        }

        private async Task RemoveDepartureWaypoint(WaypointModel waypoint)
        {
            routeModel.DepartureWaypoints.Remove(waypoint);
            if (JS is not null)
            {
                await JS.InvokeVoidAsync("setWaypointSelection", false);
            }
        }

        private void RemoveArrivalWaypoint(WaypointModel waypoint)
        {
            routeModel.ArrivalWaypoints.Remove(waypoint);
        }

        //private void CalculateRouteReductionFactor()
        //{
        //    // For demo purposes, just showing the results
        //    showResultsForReductionFactor = true;

        //    // In a real implementation, you would calculate the reduction factor here
        //    routeReductionFactor = 0.82M;
        //    routeDistance = 5952;
        //}

        private bool ValidateRouteData()
        {
            if (string.IsNullOrWhiteSpace(routeModel.RouteName))
            {
                errorMessage = "Please enter a route name.";
                return false;
            }
            if (!(routeModel.DeparturePorts.Any(p => p.Port != null) ||
                  routeModel.DepartureWaypoints.Any(w => !string.IsNullOrEmpty(w.Latitude) && !string.IsNullOrEmpty(w.Longitude))))
            {
                errorMessage = "Please provide at least one valid departure port or waypoint.";
                return false;
            }
            
            if (!(routeModel.ArrivalPorts.Any(p => p.Port != null) ||
                  routeModel.ArrivalWaypoints.Any(w => !string.IsNullOrEmpty(w.Latitude) && !string.IsNullOrEmpty(w.Longitude))))
            {
                errorMessage = "Please provide at least one valid arrival port or waypoint.";
                return false;
            }
            //if (!routeModel.DeparturePorts.Any() || routeModel.DeparturePorts.All(p => p.Port == null))
            //{
            //    errorMessage = "Please add at least one departure port.";
            //    return;
            //}

            //if (!routeModel.ArrivalPorts.Any() || routeModel.ArrivalPorts.All(p => p.Port == null))
            //{
            //    errorMessage = "Please add at least one arrival port.";
            //    return;
            //}
            if (!routeModel.ExceedanceProbability.HasValue ||
                routeModel.ExceedanceProbability <= 0 ||
                routeModel.ExceedanceProbability >= 1)
            {
                errorMessage = "Exceedance probability must be a number between 0 and 1.";
                return false;
            }

            if (string.IsNullOrEmpty(routeModel.SeasonalType))
            {
                errorMessage = "Please select a seasonal type.";
                return false;
            }

            if (string.IsNullOrEmpty(routeModel.WayType))
            {
                errorMessage = "Please select a wave type.";
                return false;
            }

            // If all is valid
            errorMessage = string.Empty;
            return true;
        }

        private RouteRequest PrepareRouteRequest()
        {
            var request = new RouteRequest
            {
                Units = "km",
                IncludePorts = true,
                OnlyTerminals = true,
                Restrictions = new[] { routeModel.SeasonalType }  // Use the seasonal type as restriction
            };

            // Set Origin coordinates
            if (routeModel.DeparturePorts.Any(p => p.Port != null))
            {
                // Use the first departure port
                var departurePort = routeModel.DeparturePorts.First(p => p.Port != null).Port;
                request.Origin = new[] { departurePort.Longitude, departurePort.Latitude };
            }
            else if (routeModel.DepartureWaypoints.Any())
            {
                // Use the first departure waypoint
                var waypoint = routeModel.DepartureWaypoints.First();
                if (double.TryParse(waypoint.Longitude, out double longitude) &&
                    double.TryParse(waypoint.Latitude, out double latitude))
                {
                    request.Origin = new[] { longitude, latitude };
                }
            }

            // Set Destination coordinates
            if (routeModel.ArrivalPorts.Any(p => p.Port != null))
            {
                // Use the first arrival port
                var arrivalPort = routeModel.ArrivalPorts.First(p => p.Port != null).Port;
                request.Destination = new[] { arrivalPort.Longitude, arrivalPort.Latitude };
            }
            else if (routeModel.ArrivalWaypoints.Any())
            {
                // Use the first arrival waypoint
                var waypoint = routeModel.ArrivalWaypoints.First();
                if (double.TryParse(waypoint.Longitude, out double longitude) &&
                    double.TryParse(waypoint.Latitude, out double latitude))
                {
                    request.Destination = new[] { longitude, latitude };
                }
            }

            return request;
        }

        private async Task CalculateRouteReductionFactor()
        {
            try
            {
                // Validate necessary data is available
                if (!ValidateRouteData())
                {
                    // Show error message to user
                    // You can implement this using a toast/notification system
                    Console.WriteLine("Please complete all required route information");
                    return;
                }

                // Prepare the RouteRequest object
                var routeRequest = PrepareRouteRequest();


                // Call the API
                //var result = await Http.PostAsJsonAsync("api/v1/RouteRequest/RouteRequest", routeRequest);
                //// Process the result
                //ProcessRouteCalculationResult(result);
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error calculating route reduction factor: {ex.Message}");
                // You can add additional error handling or user notification here
            }
        }

        private void ProcessRouteCalculationResult(dynamic result)
        {
            // Here you would process the result returned from the API
            // This might include:
            // 1. Extracting route properties
            // 2. Calculating the route reduction factor based on the result and the model parameters
            // 3. Updating the UI with the results

            if (result != null)
            {
                // Extract route length
                string routeLength = result.route_length;

                // Calculate reduction factor based on wave type and exceedance probability
                double reductionFactor = CalculateReductionFactor(
                    routeModel.WayType,
                    routeModel.ExceedanceProbability.Value,
                    result
                );

                // Update the UI or model with the calculated values
                // routeModel.ReductionFactor = reductionFactor;

                // You might want to display this information to the user
                // DisplayResults(routeLength, reductionFactor);
            }
        }
        private double CalculateReductionFactor(string waveType, double exceedanceProbability, dynamic routeData)
        {
            // Implement your reduction factor calculation logic here
            // This would depend on your specific business requirements

            // Example placeholder calculation:
            double baseReductionFactor = 1.0;

            // Adjust based on wave type
            switch (waveType)
            {
                case "High":
                    baseReductionFactor *= 0.8;
                    break;
                case "Medium":
                    baseReductionFactor *= 0.9;
                    break;
                case "Low":
                    baseReductionFactor *= 1.0;
                    break;
            }

            // Adjust based on exceedance probability
            // Lower exceedance probability typically means higher reduction
            baseReductionFactor *= (1.0 - exceedanceProbability);

            // Further adjustments based on route properties could be made here

            return baseReductionFactor;
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

