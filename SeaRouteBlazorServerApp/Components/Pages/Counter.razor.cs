using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SeaRouteModel.Models;
using System.Text.Json;

namespace SeaRouteBlazorServerApp.Components.Pages
{
    public partial class Counter
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
        [Parameter]
        public EventCallback<RouteModel> OnReportDataReady { get; set; }
        private List<string> seasonalOptions = new() { "Annual", "Spring", "Summer", "Fall", "Winter" };
        private List<string> WaytypeOptions = new() { "ABS", "BMT" };
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
        private DotNetObjectReference<ReductionFactorCalculation>? objRef;
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
        #region Departure Port Methods
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
                // Search ports from API and store in a temporary variable
                var searchResults = await SearchPortsAsync(departureLocationQuery);

                // Create a temporary PortSelectionModel to hold search results
                var tempPortSelection = new PortSelectionModel
                {
                    SearchTerm = departureLocationQuery,
                    SearchResults = searchResults
                };

                // If we have results, show them in a dropdown
                if (searchResults.Any())
                {
                    routeModel.DepartureLocation = departureLocationQuery;
                    // Store results for display
                    routeModel.MainDeparturePortSelection = tempPortSelection;
                }


            }
        }
        private async Task UpdateDeparturePortSearchDepartureLocation(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            departureLocationQuery = newPort.Name;
            // Call the JavaScript visualization after API search
            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
                await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, true);
            portSelection.SearchResults.Clear();
            //await CheckAndCalculateRoute();
            StateHasChanged();
        }
        private void AddDeparturePort()
        {
            var portModel = new PortSelectionModel { SequenceNumber = routeModel.DepartureItems.Count + 1 };

            // Add to combined list
            routeModel.DepartureItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.DepartureItems.Count + 1,
                ItemType = "Port",
                Port = portModel
            });

            // Add to ports list if you're still maintaining it
            routeModel.DeparturePorts.Add(portModel);
            StateHasChanged();
        }

        private async Task RemoveDeparturePort(PortSelectionModel port)
        {
            if (JS is not null && port.Port?.Longitude != null && port.Port?.Latitude != null)
            {
                await JS.InvokeVoidAsync("removePort", port.Port.Name, port.Port.Latitude, port.Port.Longitude);
            }
            var itemToRemove = routeModel.DepartureItems.FirstOrDefault(i => i.ItemType == "Port" && i.Port == port);
            if (itemToRemove != null)
            {
                routeModel.DepartureItems.Remove(itemToRemove);
                // Resequence remaining items
                for (int i = 0; i < routeModel.DepartureItems.Count; i++)
                {
                    routeModel.DepartureItems[i].SequenceNumber = i + 1;
                }
            }

            routeModel.DeparturePorts.Remove(port);
            StateHasChanged();
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
            if (string.IsNullOrWhiteSpace(portSelection?.SearchTerm))
            {
                return;
            }

            // Call API to get search results
            portSelection.SearchResults = await SearchPortsAsync(portSelection.SearchTerm);



            StateHasChanged();
        }

        private async Task UpdateDeparturePort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            // Call JS visualization after getting API results
            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
            {
                await JS.InvokeVoidAsync("zoomAndPinLocation", portSelection.SearchTerm, true, newPort.Latitude,
                    newPort.Longitude);

            }

            portSelection.SearchResults.Clear();
            StateHasChanged();
        }
        #endregion

        #region Arrival Port Methods
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
                // Search ports from API and store in a temporary variable
                var searchResults = await SearchPortsAsync(arrivalLocationQuery);

                // Create a temporary PortSelectionModel to hold search results
                var tempPortSelection = new PortSelectionModel
                {
                    SearchTerm = arrivalLocationQuery,
                    SearchResults = searchResults
                };

                // If we have results, show them in a dropdown
                if (searchResults.Any())
                {
                    routeModel.ArrivalLocation = arrivalLocationQuery;
                    // Store results for display
                    routeModel.MainArrivalPortSelection = tempPortSelection;
                }


            }
        }
        private async Task UpdateArrivalPortSearchArrivalLocation(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            arrivalLocationQuery = newPort.Name;
            // Call the JavaScript visualization after API search
            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
                await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, false);

            portSelection.SearchResults.Clear();
            // await CheckAndCalculateRoute();
            StateHasChanged();
        }
        private void AddArrivalPort()
        {
            var portModel = new PortSelectionModel { SequenceNumber = routeModel.ArrivalItems.Count + 1 };

            // Add to combined list
            routeModel.ArrivalItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.ArrivalItems.Count + 1,
                ItemType = "Port",
                Port = portModel
            });

            // Add to the original ports list
            routeModel.ArrivalPorts.Add(portModel);
            StateHasChanged();
        }

        private async Task RemoveArrivalPort(PortSelectionModel port)
        {
            if (JS is not null && port.Port?.Longitude != null && port.Port?.Latitude != null)
            {
                await JS.InvokeVoidAsync("removePort", port.Port.Name, port.Port.Latitude, port.Port.Longitude);
            }
            var itemToRemove = routeModel.ArrivalItems.FirstOrDefault(i => i.ItemType == "Port" && i.Port == port);
            if (itemToRemove != null)
            {
                routeModel.ArrivalItems.Remove(itemToRemove);
                // Resequence remaining items
                for (int i = 0; i < routeModel.ArrivalItems.Count; i++)
                {
                    routeModel.ArrivalItems[i].SequenceNumber = i + 1;
                }
            }
            routeModel.ArrivalPorts.Remove(port);

            StateHasChanged();
        }

        private async Task HandleArrivalEnterKey(KeyboardEventArgs e, PortSelectionModel portSelection)
        {
            if (e.Key == "Enter")
            {
                await SearchArrivalPortsForExisting(portSelection);
            }
        }

        private async Task SearchArrivalPortsForExisting(PortSelectionModel portSelection)
        {
            if (string.IsNullOrWhiteSpace(portSelection?.SearchTerm))
            {
                return;
            }

            // Call API to get search results
            portSelection.SearchResults = await SearchPortsAsync(portSelection.SearchTerm);



            StateHasChanged();
        }

        private async Task UpdateArrivalPort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            // Call JS visualization after getting API results
            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
            {
                await JS.InvokeVoidAsync("zoomAndPinLocation", portSelection.SearchTerm, false, newPort.Latitude,
                    newPort.Longitude);
            }
            portSelection.SearchResults.Clear();
            StateHasChanged();
        }
        #endregion

        // Common port search method that calls the API
        public async Task<List<PortModel>> SearchPortsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return new List<PortModel>();
            }

            try
            {
                // Call the API
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/");
                // httpClient.BaseAddress = new Uri("https://localhost:7155/");
                var response = await httpClient.GetAsync($"api/v1/portsapi/search?searchTerm={Uri.EscapeDataString(searchTerm)}");

                if (response.IsSuccessStatusCode)
                {
                    //  var Content = await response.Content.ReadAsStringAsync();
                    // var cPortResults = await response.Content.ReadFromJsonAsync<List<Port>>() ?? new List<Port>();
                    //  var results = MapToPortModels(cPortResults);
                    var results = await response.Content.ReadFromJsonAsync<List<PortModel>>() ?? new List<PortModel>();
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
        private List<PortModel> MapToPortModels(List<Port> ports)
        {
            return ports.Select(port => new PortModel
            {
                Port_Id = port.GeoPointId.ToString(),
                Name = port.PortName,
                Country_Id = port.CountryCode,
                Country = port.CountryCodeNavigation?.CountryName ?? string.Empty,
                Country_Code = port.CountryCode,
                Admiralty_Chart = string.Empty,
                Unlocode = port.Unlocode,
                Principal_Facilities = string.Empty,
                Latitude = port.GeoPoint?.Latitude ?? 0,
                Longitude = port.GeoPoint?.Longitude ?? 0,
                Port_Authority = port.PortAuthority,
                Last_Updated = port.ModifiedDate ?? port.CreatedDate
            }).ToList();
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
            if (OnReportDataReady.HasDelegate)
            {
                await OnReportDataReady.InvokeAsync(routeModel);
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
            var waypointModel = new WaypointModel { SequenceNumber = routeModel.DepartureItems.Count + 1 };

            // Add to combined list
            routeModel.DepartureItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.DepartureItems.Count + 1,
                ItemType = "Waypoint",
                Waypoint = waypointModel
            });

            // Add to waypoints list if you're still maintaining it
            routeModel.DepartureWaypoints.Add(waypointModel);

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
            var waypointModel = new WaypointModel { SequenceNumber = routeModel.ArrivalItems.Count + 1 };

            // Add to combined list
            routeModel.ArrivalItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.ArrivalItems.Count + 1,
                ItemType = "Waypoint",
                Waypoint = waypointModel
            });

            // Add to the original waypoints list
            routeModel.ArrivalWaypoints.Add(waypointModel);

            await EnableWaypointSelection();
        }

        private async Task RemoveDepartureWaypoint(WaypointModel waypoint)
        {
            if (JS is not null)
            {
                await JS.InvokeVoidAsync("setWaypointSelection", false);
                if (waypoint.Latitude != null && waypoint.Longitude != null)
                    await JS.InvokeVoidAsync("removeWaypoint", waypoint.Latitude, waypoint.Longitude);
            }
            var itemToRemove = routeModel.DepartureItems.FirstOrDefault(i => i.ItemType == "Waypoint" && i.Waypoint == waypoint);
            if (itemToRemove != null)
            {
                routeModel.DepartureItems.Remove(itemToRemove);
                // Resequence remaining items
                for (int i = 0; i < routeModel.DepartureItems.Count; i++)
                {
                    routeModel.DepartureItems[i].SequenceNumber = i + 1;
                }
            }
            routeModel.DepartureWaypoints.Remove(waypoint);

        }

        private async Task RemoveArrivalWaypoint(WaypointModel waypoint)
        {
            if (JS is not null)
            {
                await JS.InvokeVoidAsync("setWaypointSelection", false);
                if (waypoint.Latitude != null && waypoint.Longitude != null)
                    await JS.InvokeVoidAsync("removeWaypoint", waypoint.Latitude, waypoint.Longitude);
            }
            var itemToRemove = routeModel.ArrivalItems.FirstOrDefault(i => i.ItemType == "Waypoint" && i.Waypoint == waypoint);
            if (itemToRemove != null)
            {
                routeModel.ArrivalItems.Remove(itemToRemove);
                // Resequence remaining items
                for (int i = 0; i < routeModel.ArrivalItems.Count; i++)
                {
                    routeModel.ArrivalItems[i].SequenceNumber = i + 1;
                }
            }
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
        private async Task CheckAndCalculateRoute()
        {
            // Check if both departure and arrival ports are selected
            if (routeModel.MainDeparturePortSelection?.Port != null &&
                routeModel.MainArrivalPortSelection?.Port != null)
            {
                // Both ports are selected, calculate and display the route
                await CalculateRouteReductionFactor();
            }
        }

        private bool ValidateRouteData()
        {
            //if (string.IsNullOrWhiteSpace(routeModel.RouteName))
            //{
            //    errorMessage = "Please enter a route name.";
            //    return false;
            //}
            if (routeModel.MainDeparturePortSelection.Port == null)
            {
                errorMessage = "Please select a departure port.";
                return false;
            }
            if (routeModel.MainArrivalPortSelection.Port == null)
            {
                errorMessage = "Please select an arrival port.";
                return false;
            }
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
        // test
        public class RoutePointModel
        {
            public string Type { get; set; }
            public double[] LatLng { get; set; }
            public string Name { get; set; }
        }
        public async Task CalculateMultiSegmentRoute()
        {
            try
            {
                // Initialize route calculation on JS side but don't use its data
                await JS.InvokeVoidAsync("initializeRouteCalculation");

                // Get route points from JS for ordering/sequence purposes only
                var routePoints = await JS.InvokeAsync<List<RoutePointModel>>("getRouteData");
                if (routePoints == null || routePoints.Count < 2)
                {
                    return; // Need at least 2 points for a route
                }

                // Create a mapping of all points for processing
                Dictionary<string, RoutePointRef> pointMapping = new Dictionary<string, RoutePointRef>();

                // [Your existing code for adding points to pointMapping]

                // Process each segment using route points order from JS but data from C#
                List<RoutePointRef> orderedPoints = new List<RoutePointRef>();

                // [Your existing code for creating orderedPoints]

                // List to store segment information
                List<RouteSegmentInfo> routeSegments = new List<RouteSegmentInfo>();
                double totalDistance = 0;
                double totalDuration = 0;

                // Process each segment sequentially using our ordered C# data
                for (int i = 0; i < orderedPoints.Count - 1; i++)
                {
                    var origin = orderedPoints[i];
                    var destination = orderedPoints[i + 1];

                    // Create route request for this segment
                    var segmentRequest = new RouteRequest
                    {
                        Origin = new double[] { origin.Longitude, origin.Latitude },
                        Destination = new double[] { destination.Longitude, destination.Latitude },
                        Restrictions = new string[] { "northwest" },
                        IncludePorts = true,
                        Units = "km",
                        OnlyTerminals = true
                    };

                    // Call API for this segment
                    using var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/");
                    var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", segmentRequest);

                    if (result.IsSuccessStatusCode)
                    {
                        var jsonString = await result.Content.ReadAsStringAsync();
                        using var jsonDoc = JsonDocument.Parse(jsonString);
                        var root = jsonDoc.RootElement;

                        // Check if route object exists
                        if (root.TryGetProperty("route", out var routeElement) &&
                            routeElement.TryGetProperty("properties", out var propertiesElement))
                        {
                            // Extract segment distance and duration
                            double segmentDistance = 0;
                            double segmentDuration = 0;
                            string units = "km";

                            if (propertiesElement.TryGetProperty("length", out var lengthElement))
                            {
                                segmentDistance = lengthElement.GetDouble();
                                totalDistance += segmentDistance;
                            }

                            if (propertiesElement.TryGetProperty("duration_hours", out var durationElement))
                            {
                                segmentDuration = durationElement.GetDouble();
                                totalDuration += segmentDuration;
                            }

                            if (propertiesElement.TryGetProperty("units", out var unitsElement))
                            {
                                units = unitsElement.GetString();
                            }

                            // Store segment information
                            var segmentInfo = new RouteSegmentInfo
                            {
                                SegmentIndex = i,
                                StartPointName = origin.Name,
                                EndPointName = destination.Name,
                                Distance = segmentDistance,
                                DurationHours = segmentDuration,
                                Units = units,
                                StartCoordinates = new double[] { origin.Longitude, origin.Latitude },
                                EndCoordinates = new double[] { destination.Longitude, destination.Latitude }
                            };

                            routeSegments.Add(segmentInfo);

                            // Get the raw JSON for the route
                            var routeJson = routeElement.GetRawText();

                            // Pass segment info to JavaScript along with route data
                            await JS.InvokeVoidAsync("processRouteSegmentWithInfo",
                                routeJson,
                                i,
                                orderedPoints.Count - 1,
                                segmentInfo.StartPointName,
                                segmentInfo.EndPointName,
                                segmentDistance,
                                segmentDuration);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error calculating route segment {i}: {result.StatusCode}");
                    }
                }

                // Save route segments information to the route model
                SaveRouteSegmentsInfo(routeSegments, totalDistance, totalDuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating multi-segment route: {ex.Message}");
            }
        }
        public class RouteSegmentInfo
        {
            public int SegmentIndex { get; set; }
            public string StartPointName { get; set; }
            public string EndPointName { get; set; }
            public double Distance { get; set; }
            public double DurationHours { get; set; }
            public string Units { get; set; } = "km";
            public double[] StartCoordinates { get; set; } // [longitude, latitude]
            public double[] EndCoordinates { get; set; } // [longitude, latitude]
        }
        private void SaveRouteSegmentsInfo(List<RouteSegmentInfo> segments, double totalDistance, double totalDuration)
        {
            // Add these properties to your RouteModel class if they don't exist
            routeModel.RouteSegments = segments;
            routeModel.TotalDistance = totalDistance;
            routeModel.TotalDurationHours = totalDuration;

            // Notify UI of changes
            StateHasChanged();
        }
        //public async Task RemoveRouteSegment(int segmentIndex)
        //{
        //    if (routeModel.RouteSegments != null && segmentIndex < routeModel.RouteSegments.Count)
        //    {
        //        // Update total distance and duration
        //        routeModel.TotalDistance -= routeModel.RouteSegments[segmentIndex].Distance;
        //        routeModel.TotalDurationHours -= routeModel.RouteSegments[segmentIndex].DurationHours;

        //        // Remove the segment from C# model
        //        routeModel.RouteSegments.RemoveAt(segmentIndex);

        //        // Re-index remaining segments
        //        for (int i = segmentIndex; i < routeModel.RouteSegments.Count; i++)
        //        {
        //            routeModel.RouteSegments[i].SegmentIndex = i;
        //        }

        //        // Remove from JavaScript as well
        //        await JS.InvokeVoidAsync("removeRouteSegment", segmentIndex);

        //        // Update UI
        //        StateHasChanged();
        //    }
        //}
        //public async Task CalculateMultiSegmentRoute()
        //{
        //    try
        //    {
        //        // Initialize route calculation on JS side but don't use its data
        //        await JS.InvokeVoidAsync("initializeRouteCalculation");

        //        // Get route points from JS for ordering/sequence purposes only
        //        var routePoints = await JS.InvokeAsync<List<RoutePointModel>>("getRouteData");
        //        if (routePoints == null || routePoints.Count < 2)
        //        {
        //            return; // Need at least 2 points for a route
        //        }

        //        // Create a mapping of all points for processing
        //        Dictionary<string, RoutePointRef> pointMapping = new Dictionary<string, RoutePointRef>();

        //        // Add departure port
        //        if (routeModel.MainDeparturePortSelection?.Port != null)
        //        {
        //            pointMapping["departure"] = new RoutePointRef
        //            {
        //                Type = "departure",
        //                Latitude = routeModel.MainDeparturePortSelection.Port.Latitude,
        //                Longitude = routeModel.MainDeparturePortSelection.Port.Longitude,
        //                Name = routeModel.MainDeparturePortSelection.Port.Name
        //            };
        //        }

        //        // Add arrival port
        //        if (routeModel.MainArrivalPortSelection?.Port != null)
        //        {
        //            pointMapping["arrival"] = new RoutePointRef
        //            {
        //                Type = "arrival",
        //                Latitude = routeModel.MainArrivalPortSelection.Port.Latitude,
        //                Longitude = routeModel.MainArrivalPortSelection.Port.Longitude,
        //                Name = routeModel.MainArrivalPortSelection.Port.Name
        //            };
        //        }

        //        // Add intermediate departure ports
        //        for (int i = 0; i < routeModel.DeparturePorts.Count; i++)
        //        {
        //            var port = routeModel.DeparturePorts[i];
        //            if (port.Port != null)
        //            {
        //                pointMapping[$"departurePort{i}"] = new RoutePointRef
        //                {
        //                    Type = "port",
        //                    Latitude = port.Port.Latitude,
        //                    Longitude = port.Port.Longitude,
        //                    Name = port.Port.Name
        //                };
        //            }
        //        }

        //        // Add intermediate arrival ports
        //        for (int i = 0; i < routeModel.ArrivalPorts.Count; i++)
        //        {
        //            var port = routeModel.ArrivalPorts[i];
        //            if (port.Port != null)
        //            {
        //                pointMapping[$"arrivalPort{i}"] = new RoutePointRef
        //                {
        //                    Type = "port",
        //                    Latitude = port.Port.Latitude,
        //                    Longitude = port.Port.Longitude,
        //                    Name = port.Port.Name
        //                };
        //            }
        //        }

        //        // Process waypoints from C# models
        //        // Add departure waypoints
        //        for (int i = 0; i < routeModel.DepartureWaypoints.Count; i++)
        //        {
        //            var waypoint = routeModel.DepartureWaypoints[i];
        //            if (double.TryParse(waypoint.Latitude, out double lat) &&
        //                double.TryParse(waypoint.Longitude, out double lng))
        //            {
        //                pointMapping[$"departureWaypoint{i}"] = new RoutePointRef
        //                {
        //                    Type = "waypoint",
        //                    Latitude = lat,
        //                    Longitude = lng,
        //                    Name = $"Waypoint {i + 1}"
        //                };
        //            }
        //        }

        //        // Add arrival waypoints
        //        for (int i = 0; i < routeModel.ArrivalWaypoints.Count; i++)
        //        {
        //            var waypoint = routeModel.ArrivalWaypoints[i];
        //            if (double.TryParse(waypoint.Latitude, out double lat) &&
        //                double.TryParse(waypoint.Longitude, out double lng))
        //            {
        //                pointMapping[$"arrivalWaypoint{i}"] = new RoutePointRef
        //                {
        //                    Type = "waypoint",
        //                    Latitude = lat,
        //                    Longitude = lng,
        //                    Name = $"Waypoint {i + 1}"
        //                };
        //            }
        //        }

        //        // Process each segment using route points order from JS but data from C#
        //        List<RoutePointRef> orderedPoints = new List<RoutePointRef>();

        //        // Use JS route points to determine order but get actual data from C# models
        //        foreach (var jsPoint in routePoints)
        //        {
        //            RoutePointRef point = null;

        //            // Try to match point from JS with our C# data
        //            if (jsPoint.Type == "departure" && pointMapping.ContainsKey("departure"))
        //            {
        //                point = pointMapping["departure"];
        //            }
        //            else if (jsPoint.Type == "arrival" && pointMapping.ContainsKey("arrival"))
        //            {
        //                point = pointMapping["arrival"];
        //            }
        //            else if (jsPoint.Type == "waypoint")
        //            {
        //                // Find closest waypoint in our mapping by comparing coordinates
        //                string closestKey = null;
        //                double minDistance = double.MaxValue;

        //                foreach (var kvp in pointMapping)
        //                {
        //                    if (kvp.Value.Type == "waypoint")
        //                    {
        //                        double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
        //                                      Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
        //                        if (dist < minDistance)
        //                        {
        //                            minDistance = dist;
        //                            closestKey = kvp.Key;
        //                        }
        //                    }
        //                }

        //                if (closestKey != null)
        //                {
        //                    point = pointMapping[closestKey];
        //                }
        //            }
        //            else if (jsPoint.Type == "port")
        //            {
        //                // Find closest port in our mapping
        //                string closestKey = null;
        //                double minDistance = double.MaxValue;

        //                foreach (var kvp in pointMapping)
        //                {
        //                    if (kvp.Value.Type == "port")
        //                    {
        //                        double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
        //                                      Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
        //                        if (dist < minDistance)
        //                        {
        //                            minDistance = dist;
        //                            closestKey = kvp.Key;
        //                        }
        //                    }
        //                }

        //                if (closestKey != null)
        //                {
        //                    point = pointMapping[closestKey];
        //                }
        //            }

        //            if (point != null)
        //            {
        //                orderedPoints.Add(point);
        //            }
        //        }

        //        // Process each segment sequentially using our ordered C# data
        //        for (int i = 0; i < orderedPoints.Count - 1; i++)
        //        {
        //            var origin = orderedPoints[i];
        //            var destination = orderedPoints[i + 1];

        //            // Create route request for this segment
        //            var segmentRequest = new RouteRequest
        //            {
        //                // Use coordinates from C# model
        //                Origin = new double[] { origin.Longitude, origin.Latitude },
        //                Destination = new double[] { destination.Longitude, destination.Latitude },
        //                Restrictions = new string[] { "northwest" },
        //                IncludePorts = true,
        //                Units = "km",
        //                OnlyTerminals = true
        //            };

        //            // Call API for this segment
        //            // Call the API
        //            using var httpClient = new HttpClient();
        //            httpClient.BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/");
        //            // httpClient.BaseAddress = new Uri("https://localhost:7155/");
        //            var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", segmentRequest);
        //            if (result.IsSuccessStatusCode)
        //            {
        //                var jsonString = await result.Content.ReadAsStringAsync();
        //                using var jsonDoc = JsonDocument.Parse(jsonString);
        //                var root = jsonDoc.RootElement;

        //                // Check if route object exists
        //                if (root.TryGetProperty("route", out var routeElement))
        //                {
        //                    // Get the raw JSON for the route
        //                    var routeJson = routeElement.GetRawText();

        //                    // Send to JavaScript to process this segment
        //                    await JS.InvokeVoidAsync("processRouteSegment", routeJson, i, orderedPoints.Count - 1);
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine($"Error calculating route segment {i}: {result.StatusCode}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error calculating multi-segment route: {ex.Message}");
        //    }
        //}

        // Helper class to store route point information from C# models
        private class RoutePointRef
        {
            public string Type { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
        }

        private RouteRequest PrepareRouteRequest()
        {
            // Create a route request from the current model
            var request = new RouteRequest
            {
                // Set origin from main departure port coordinates
                Origin = new double[] {
            routeModel.MainDeparturePortSelection.Port.Longitude,
            routeModel.MainDeparturePortSelection.Port.Latitude
        },

                // Set destination from main arrival port coordinates
                Destination = new double[] {
            routeModel.MainArrivalPortSelection.Port.Longitude,
            routeModel.MainArrivalPortSelection.Port.Latitude
        },

                // Set restrictions if any (e.g. "piracy")
                Restrictions = new string[] { "northwest" },

                // Set other options
                IncludePorts = true,
                Units = "km",
                OnlyTerminals = false
            };



            return request;
        }

        private async Task CalculateRouteReductionFactorReport()
        {
            if (!ValidateRouteData())
            {
                // Show error message to user
                // You can implement this using a toast/notification system
                Console.WriteLine("Please complete all required route information");
                return;
            }
            showResultsForReductionFactor = true;
            await Task.CompletedTask;
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
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api-ngea-rf-dev-001.azurewebsites.net/");
                // httpClient.BaseAddress = new Uri("https://localhost:7155/");
                var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", routeRequest);
                await ProcessRouteCalculationResult(result);
                var reductionFactor = CalculateReductionFactor(routeModel.WayType, routeModel.ExceedanceProbability ?? 0, result);
                // showResultsForReductionFactor = true;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine($"Error calculating route reduction factor: {ex.Message}");
                // You can add additional error handling or user notification here
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

        private double CalculateReductionFactor(string waveType, double exceedanceProbability, dynamic routeData)
        {
            // Base reduction factor (you can adjust this based on your requirements)
            double baseFactor = 1.0;

            // Apply adjustments based on wave type
            switch (waveType.ToLower())
            {
                case "high":
                    baseFactor *= 0.8; // 20% reduction for high waves
                    break;
                case "medium":
                    baseFactor *= 0.9; // 10% reduction for medium waves
                    break;
                case "low":
                    // No reduction for low waves
                    break;
                default:
                    // Default case, no adjustment
                    break;
            }

            // Apply adjustments based on exceedance probability
            // Higher probability generally means greater risk, so reduce the factor more
            if (exceedanceProbability > 0)
            {
                baseFactor *= (1 - (exceedanceProbability / 100));
            }

            // Consider route length for additional adjustments if needed
            var distance = routeData.GetProperty("distance").GetDouble();
            if (distance > 1000) // For routes longer than 1000 km
            {
                baseFactor *= 0.95; // 5% additional reduction for long routes
            }

            return baseFactor;
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
