using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using NextGenEngApps.DigitalRules.CRoute.Models;
using NextGenEngApps.DigitalRules.CRoute.Services.API.Request;
using PdfSharpCore.Pdf;
using System.Text.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using ReductionFactor = NextGenEngApps.DigitalRules.CRoute.DAL.Models.ReductionFactor;

namespace NextGenEngApps.DigitalRules.CRoute.Components.Pages
{
    public partial class ReductionFactorCalculation
    {
        [Inject]
        public IConfiguration Configuration { get; set; } = default!;

        [Parameter]
        public EventCallback OnBack { get; set; }
        [Parameter]
        public EventCallback<string> OnAddEditVessel { get; set; }
        [Parameter]
        public EventCallback<string> OnShowAbsReport { get; set; }
        [Parameter]
        public EventCallback OnShowReportForReductionFactor { get; set; }
        [Parameter]
        public EventCallback<(double, double)> OnCoordinatesCaptured { get; set; }
        [Parameter]
        public EventCallback<RouteModel> OnReportDataReady { get; set; }
        [Parameter]
        public EventCallback<List<RouteLegModel>> OnLegsDataReady { get; set; }

        [Parameter]
        public string EditRouteId { get; set; }

        private List<string> seasonalOptions = new() { "Annual", "Spring", "Summer", "Fall", "Winter" };
        private Dictionary<string, List<string>> seasonMonths = new()
        {
         { "Spring", new List<string> { "Mar", "May" } },
         { "Summer", new List<string> { "Jun",  "Aug" } },
         { "Fall", new List<string> { "Sep",  "Nov" } },
         { "Winter", new List<string> { "Dec", "Feb" } }
        };
        private List<string> WaytypeOptions = new() { "ABS", "BMT" };
        private bool showDropdown = false;
        private bool showDropdownforwaypoint = false;
        private bool isValidExceedanceProbability = true;
        private ReductionFactor reductionFactor = new ReductionFactor();
        private bool isLoading = false;
        private string errorMessage = string.Empty;
        private void CloseOverlay() { /* Logic to hide overlay */ }
        private decimal? routeReductionFactor { get; set; }
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
        private bool isSeasonFixed = false;
        private List<RouteLegModel> routeLegs = new List<RouteLegModel>();
        private List<GeoPointCoordinate> AllCoordinates = new List<GeoPointCoordinate>();
        private List<Coordinate> extractedCoordinates = new List<Coordinate>();
        // Add a field to store the split voyage legs
        private List<VoyageLeg> voyageLegs = new List<VoyageLeg>();
        private VoyageLegReductionFactorResponse reductionFactorResponse;
        protected async override Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(EditRouteId))
                await RestoreRoute(EditRouteId);

            await Task.CompletedTask;
            await GetSampleports();

        }

        private string GetCorrectedReductionFactor(decimal originalFactor, string seasonType)
        {

            //if (seasonType == "Annual" || seasonType == "Winter")
            //{
            //    originalFactor = Math.Max(originalFactor, 0.80m);
            //}

            originalFactor = Math.Max(originalFactor, 0.80m);

            decimal correctedFactor;

            switch (seasonType)
            {
                case "Summer":
                    correctedFactor = originalFactor * 0.9m;
                    break;
                case "Spring":
                    correctedFactor = originalFactor * 0.93m;
                    break;
                case "Fall":
                    correctedFactor = originalFactor * 0.97m;
                    break;
                case "Annual":
                case "Winter":
                default:
                    correctedFactor = originalFactor;
                    break;
            }


            if (seasonType == "Annual" || seasonType == "Winter")


            {
                correctedFactor = Math.Max(correctedFactor, 0.80m);
            }

            return correctedFactor.ToString("F2");
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


        private void OnSearchInputChanged(ChangeEventArgs e)
        {
            departureLocationQuery = e.Value?.ToString();

            _debounceService.Debounce(async () =>
            {
                await InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(departureLocationQuery))
                    {
                        await SearchDepartureLocation();
                        StateHasChanged();
                    }
                    else
                    {
                        if (routeModel.MainDeparturePortSelection != null)
                        {
                            routeModel.MainDeparturePortSelection.SearchResults?.Clear();
                            StateHasChanged();
                        }
                    }
                });
            });
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
                if (newPort.Latitude != 0 && newPort.Longitude != 0)
                {

                    await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, true, newPort.Latitude, newPort.Longitude);
                }
                else
                {

                    await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, true);
                }
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
                ItemType = "P",
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
            var itemToRemove = routeModel.DepartureItems.FirstOrDefault(i => i.ItemType == "P" && i.Port == port);
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


        private void HandleDepartureInputChanged(PortSelectionModel portSelection)
        {
            _debounceService.Debounce(async () =>
            {
                await InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(portSelection?.SearchTerm))
                    {
                        portSelection.SearchResults = await SearchPortsAsync(portSelection.SearchTerm);
                    }
                    else
                    {
                        portSelection?.SearchResults?.Clear();
                    }

                    StateHasChanged();
                });
            });
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
        private void OnArrivalSearchInputChanged(ChangeEventArgs e)
        {
            arrivalLocationQuery = e.Value?.ToString();

            _debounceService.Debounce(async () =>
            {
                await InvokeAsync(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(arrivalLocationQuery))
                    {
                        await SearchArrivalLocation();
                        StateHasChanged();
                    }
                    else
                    {
                        if (routeModel.MainArrivalPortSelection != null)
                        {
                            routeModel.MainArrivalPortSelection.SearchResults?.Clear();
                            StateHasChanged();
                        }
                    }
                });
            });
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
                if (newPort.Latitude != 0 && newPort.Longitude != 0)
                {

                    await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, false, newPort.Latitude, newPort.Longitude);
                }
                else
                {

                    await JS.InvokeVoidAsync("searchLocation", portSelection.SearchTerm, false);
                }

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
                ItemType = "P",
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
            var itemToRemove = routeModel.ArrivalItems.FirstOrDefault(i => i.ItemType == "P" && i.Port == port);
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
                httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
                //httpClient.BaseAddress = new Uri("https://localhost:7155/");
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
                await OnAddEditVessel.InvokeAsync(routeModel.RouteId);
            }
        }
        private async Task ShowAbsReport()
        {
            if (OnShowAbsReport.HasDelegate)
            {
                await OnShowAbsReport.InvokeAsync(routeModel.RouteId);
            }
            if (OnReportDataReady.HasDelegate)
            {
                await OnReportDataReady.InvokeAsync(routeModel);
            }
            await Task.Delay(100);
            await CaptureMap();
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
            isRouteSaved = false; // Show the message
            await AddDepartureGeoPoints();
            string result = await routeAPIService.SaveRouteAsync(GetInputtoSaveRoute());
            if (!string.IsNullOrEmpty(result))
            {
                routeModel.RouteId = result;
                isRouteSaved = true;
                StateHasChanged();   // Update the UI
            }
            await Task.CompletedTask;
            //StateHasChanged();
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
            var waypointModel = new WaypointModel
            {
                SequenceNumber = routeModel.DepartureItems.Count + 1,
                PointId = Guid.NewGuid().ToString()
            };

            // Add to combined list
            routeModel.DepartureItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.DepartureItems.Count + 1,
                ItemType = "W",
                Waypoint = waypointModel
            });

            // Add to waypoints list if you're still maintaining it
            routeModel.DepartureWaypoints.Add(waypointModel);

            await EnableWaypointSelection();

            var wp = routeModel.DepartureWaypoints.FirstOrDefault(wp => wp.PointId == waypointModel.PointId);

            //need to add waypoint to geo_points table as we need to maintain in waypoints table
            //await routeAPIService.AddGeoPointAsync(waypointModel.PointId, double.Parse(wp.Latitude), wp.Longitude);
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
            var waypointModel = new WaypointModel
            {
                SequenceNumber = routeModel.ArrivalItems.Count + 1,
                PointId = Guid.NewGuid().ToString()
            };

            // Add to combined list
            routeModel.ArrivalItems.Add(new RouteItemModel
            {
                SequenceNumber = routeModel.ArrivalItems.Count + 1,
                ItemType = "W",
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
            var itemToRemove = routeModel.DepartureItems.FirstOrDefault(i => i.ItemType == "W" && i.Waypoint == waypoint);
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
            var itemToRemove = routeModel.ArrivalItems.FirstOrDefault(i => i.ItemType == "W" && i.Waypoint == waypoint);
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
        private async Task CheckAndCalculateRoute()
        {
            // Check if both departure and arrival ports are selected
            if (routeModel.MainDeparturePortSelection?.Port != null &&
                routeModel.MainArrivalPortSelection?.Port != null)
            {
                // Both ports are selected, calculate and display the route
                await CalculateRouteReductionFactor();
                StateHasChanged();
                await JS.InvokeVoidAsync("ScrollToRF");
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
            //if (!routeModel.ExceedanceProbability.HasValue ||
            //    routeModel.ExceedanceProbability <= 0 ||
            //    routeModel.ExceedanceProbability >= 1)
            //{
            //    errorMessage = "Exceedance probability must be a number between 0 and 1.";
            //    return false;
            //}

            //if (string.IsNullOrEmpty(routeModel.SeasonalType))
            //{
            //    errorMessage = "Please select a seasonal type.";
            //    return false;
            //}

            //if (string.IsNullOrEmpty(routeModel.WayType))
            //{
            //    errorMessage = "Please select a wave type.";
            //    return false;
            //}

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

                List<RouteSegmentInfo> routeSegments = new List<RouteSegmentInfo>();
                double totalDistance = 0;
                double totalDuration = 0;
                // modified Sireesha: Store each segment's coordinates
                var segmentCoordinatesList = new List<List<double[]>>();

                // Add departure port
                if (routeModel.MainDeparturePortSelection?.Port != null)
                {
                    pointMapping["departure"] = new RoutePointRef
                    {
                        Type = "departure",
                        Latitude = routeModel.MainDeparturePortSelection.Port.Latitude,
                        Longitude = routeModel.MainDeparturePortSelection.Port.Longitude,
                        Name = routeModel.MainDeparturePortSelection.Port.Name,
                        PointId = routeModel.MainDeparturePortSelection.Port.Port_Id
                    };
                }

                // Add arrival port
                if (routeModel.MainArrivalPortSelection?.Port != null)
                {
                    pointMapping["arrival"] = new RoutePointRef
                    {
                        Type = "arrival",
                        Latitude = routeModel.MainArrivalPortSelection.Port.Latitude,
                        Longitude = routeModel.MainArrivalPortSelection.Port.Longitude,
                        Name = routeModel.MainArrivalPortSelection.Port.Name,
                        PointId = routeModel.MainArrivalPortSelection.Port.Port_Id
                    };
                }

                // Add intermediate departure ports
                for (int i = 0; i < routeModel.DeparturePorts.Count; i++)
                {
                    var port = routeModel.DeparturePorts[i];
                    if (port.Port != null)
                    {
                        pointMapping[$"departurePort{i}"] = new RoutePointRef
                        {
                            Type = "port",
                            Latitude = port.Port.Latitude,
                            Longitude = port.Port.Longitude,
                            Name = port.Port.Name,
                            PointId = port.Port.Port_Id
                        };
                    }
                }

                // Add intermediate arrival ports
                for (int i = 0; i < routeModel.ArrivalPorts.Count; i++)
                {
                    var port = routeModel.ArrivalPorts[i];
                    if (port.Port != null)
                    {
                        pointMapping[$"arrivalPort{i}"] = new RoutePointRef
                        {
                            Type = "port",
                            Latitude = port.Port.Latitude,
                            Longitude = port.Port.Longitude,
                            Name = port.Port.Name,
                            PointId = port.Port.Port_Id
                        };
                    }
                }

                // Process waypoints from C# models
                // Add departure waypoints
                for (int i = 0; i < routeModel.DepartureWaypoints.Count; i++)
                {
                    var waypoint = routeModel.DepartureWaypoints[i];
                    if (double.TryParse(waypoint.Latitude, out double lat) &&
                        double.TryParse(waypoint.Longitude, out double lng))
                    {
                        pointMapping[$"departureWaypoint{i}"] = new RoutePointRef
                        {
                            Type = "waypoint",
                            Latitude = lat,
                            Longitude = lng,
                            Name = $"Waypoint {i + 1}",
                            PointId = waypoint.PointId
                        };
                        //need to add waypoint to geo_points table as we need to maintain in waypoints table
                        //await routeAPIService.AddGeoPointAsync(waypoint.PointId, lat, lng);
                    }
                }

                // Add arrival waypoints
                for (int i = 0; i < routeModel.ArrivalWaypoints.Count; i++)
                {
                    var waypoint = routeModel.ArrivalWaypoints[i];
                    if (double.TryParse(waypoint.Latitude, out double lat) &&
                        double.TryParse(waypoint.Longitude, out double lng))
                    {
                        pointMapping[$"arrivalWaypoint{i}"] = new RoutePointRef
                        {
                            Type = "waypoint",
                            Latitude = lat,
                            Longitude = lng,
                            Name = $"Waypoint {i + 1}",
                            PointId = waypoint.PointId
                        };
                    }
                }

                // Process each segment using route points order from JS but data from C#
                List<RoutePointRef> orderedPoints = new List<RoutePointRef>();

                // Use JS route points to determine order but get actual data from C# models
                foreach (var jsPoint in routePoints)
                {
                    RoutePointRef point = null;

                    // Try to match point from JS with our C# data
                    if (jsPoint.Type == "departure" && pointMapping.ContainsKey("departure"))
                    {
                        point = pointMapping["departure"];
                    }
                    else if (jsPoint.Type == "arrival" && pointMapping.ContainsKey("arrival"))
                    {
                        point = pointMapping["arrival"];
                    }
                    else if (jsPoint.Type == "waypoint")
                    {
                        // Find closest waypoint in our mapping by comparing coordinates
                        string closestKey = null;
                        double minDistance = double.MaxValue;

                        foreach (var kvp in pointMapping)
                        {
                            if (kvp.Value.Type == "waypoint")
                            {
                                double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
                                              Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
                                if (dist < minDistance)
                                {
                                    minDistance = dist;
                                    closestKey = kvp.Key;
                                }
                            }
                        }

                        if (closestKey != null)
                        {
                            point = pointMapping[closestKey];
                        }
                    }
                    else if (jsPoint.Type == "port")
                    {
                        // Find closest port in our mapping
                        string closestKey = null;
                        double minDistance = double.MaxValue;

                        foreach (var kvp in pointMapping)
                        {
                            if (kvp.Value.Type == "port")
                            {
                                double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
                                              Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
                                if (dist < minDistance)
                                {
                                    minDistance = dist;
                                    closestKey = kvp.Key;
                                }
                            }
                        }

                        if (closestKey != null)
                        {
                            point = pointMapping[closestKey];
                        }
                    }

                    if (point != null)
                    {
                        orderedPoints.Add(point);
                    }
                }
                bool hasDeparture = orderedPoints.Count > 0 &&
                         (orderedPoints[0].Type == "departure" || routePoints[0].Type == "departure");
                if (hasDeparture && orderedPoints.Count >= 2)
                {
                    // Process each segment sequentially using our ordered C# data
                    for (int i = 0; i < orderedPoints.Count - 1; i++)
                    {
                        var origin = orderedPoints[i];
                        var destination = orderedPoints[i + 1];

                        var segmentRequest = new RouteRequest
                        {
                            Origin = new double[] { origin.Longitude, origin.Latitude },
                            Destination = new double[] { destination.Longitude, destination.Latitude },
                            Restrictions = new string[] { "northwest" },
                            include_ports = false,
                            Units = "nm",
                            only_terminals = true
                        };

                        using var httpClient = new HttpClient();
                        httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
                        var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", segmentRequest);
                        if (result.IsSuccessStatusCode)
                        {
                            var jsonString = await result.Content.ReadAsStringAsync();
                            using var jsonDoc = JsonDocument.Parse(jsonString);
                            var root = jsonDoc.RootElement;
                            // modified Sireesha: Use a local variable for this segment's coordinates
                            var segmentCoords = new List<double[]>();
                            if (root.TryGetProperty("route", out var routeElement) &&
                                routeElement.TryGetProperty("properties", out var propertiesElement))
                            {
                                double segmentDistance = 0;
                                double segmentDuration = 0;
                                string units = "km";

                                if (routeElement.TryGetProperty("geometry", out var geometryElement) &&
                                    geometryElement.TryGetProperty("coordinates", out var coordinatesElement) &&
                                    coordinatesElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var coord in coordinatesElement.EnumerateArray())
                                    {
                                        if (coord.ValueKind == JsonValueKind.Array && coord.GetArrayLength() >= 2)
                                        {
                                            var longitude = coord[0].GetDouble();
                                            var latitude = coord[1].GetDouble();
                                            segmentCoords.Add(new double[] { longitude, latitude }); // modified Sireesha
                                        }
                                    }
                                }
                                // modified Sireesha: Add this segment's coordinates to the list
                                segmentCoordinatesList.Add(segmentCoords);

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

                                var segmentInfo = new RouteSegmentInfo
                                {
                                    SegmentIndex = i,
                                    StartPointName = origin.Name,
                                    EndPointName = destination.Name,
                                    Distance = segmentDistance,
                                    DurationHours = segmentDuration,
                                    Units = units,
                                    StartCoordinates = new double[] { origin.Longitude, origin.Latitude },
                                    EndCoordinates = new double[] { destination.Longitude, destination.Latitude },
                                    StartPointId = origin.PointId,
                                    EndPointId = destination.PointId,
                                };

                                routeSegments.Add(segmentInfo);

                                var routeJson = routeElement.GetRawText();
                                await JS.InvokeVoidAsync("processRouteSegment", routeJson, i, orderedPoints.Count - 1);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error calculating route segment {i}: {result.StatusCode}");
                            // modified Sireesha: still add an empty segment to keep indices aligned
                            segmentCoordinatesList.Add(new List<double[]>());
                        }
                    }
                }

                routeModel.RouteSegments = routeSegments;
                routeModel.TotalDistance = totalDistance;
                routeModel.TotalDurationHours = totalDuration;

                var routePointInputs = new List<RoutePointInput>();
                for (int i = 0; i < orderedPoints.Count; i++)
                {
                    var point = orderedPoints[i];
                    double segmentDistance = 0;
                    List<double[]> segmentCoordinates = new List<double[]>();
                    if (i < routeSegments.Count)
                    {
                        var seg = routeSegments[i];
                        segmentDistance = seg.Distance;
                        // modified Sireesha: Use the correct segment's coordinates
                        segmentCoordinates = segmentCoordinatesList[i];
                    }
                    routePointInputs.Add(new RoutePointInput
                    {
                        Type = point.Type == "departure" ? "port" : point.Type,
                        Name = point.Name,
                        LatLng = new double[] { point.Latitude, point.Longitude },
                        SegmentDistance = segmentDistance,
                        SegmentCoordinates = segmentCoordinates
                    });
                }
                // modified Sireesha: Now call the split method with correct segment coordinates
                voyageLegs = SplitRouteIntoVoyageLegs(routePointInputs);
                // --- Populate routeLegs for the UI ---
                routeLegs.Clear();
                if (voyageLegs != null && voyageLegs.Count > 0)
                {
                    foreach (var leg in voyageLegs)
                    {
                        routeLegs.Add(new RouteLegModel
                        {
                            DeparturePort = leg.DeparturePort,
                            ArrivalPort = leg.ArrivalPort,
                            Distance = leg.Distance
                            // ReductionFactor will be set after the API call
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating multi-segment route: {ex.Message}");
            }
        }
        private static List<double[]> ConvertCoordinatesToDoubleArray(List<Coordinate> coordinates)
        {
            var result = new List<double[]>();
            foreach (var coord in coordinates)
            {
                result.Add(new double[] { coord.Longitude, coord.Latitude });
            }
            return result;
        }


        private class RoutePointRef
        {
            public string Type { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
            public string PointId { get; set; }
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
                include_ports = false,
                Units = "nm",
                only_terminals = true
            };



            return request;
        }

        private async Task CalculateRouteReductionFactorReport()
        {
            try
            {
                isLoading = true;
                if (!ValidateRouteData())
                {
                    // Show error message to user
                    // You can implement this using a toast/notification system
                    Console.WriteLine("Please complete all required route information");
                    isLoading = false;
                    return;
                }
                await CheckAndCalculateRoute();
                if (OnLegsDataReady.HasDelegate)
                {
                    await OnLegsDataReady.InvokeAsync(routeLegs);
                }
                showResultsForReductionFactor = true;

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                isLoading = false;
            }

        }
        private List<Coordinate> RemoveDuplicateCoordinates(List<Coordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count <= 1)
                return coordinates;

            List<Coordinate> result = new List<Coordinate>();
            Coordinate previous = null;

            foreach (var current in coordinates)
            {

                if (previous != null &&
                    Math.Abs(previous.Latitude - current.Latitude) < 0.000001 &&
                    Math.Abs(previous.Longitude - current.Longitude) < 0.000001)
                {
                    continue;
                }

                result.Add(current);
                previous = current;
            }

            return result;
        }
        private async Task CalculateRouteReductionFactor()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Validate necessary data is available
                if (!ValidateRouteData())
                {
                    Console.WriteLine("Please complete all required route information");
                    return;
                }
                
                if (voyageLegs != null && voyageLegs.Count > 0)
                {
                    await CalculateUsingVoyageLegs();
                }
                else if (extractedCoordinates != null && extractedCoordinates.Any())
                {
                    await ShowRouteItems(extractedCoordinates);
                    showResultsForReductionFactor = true;
                }
                else
                {
                    // Fallback: use the old logic
                    var routeRequest = PrepareRouteRequest();
                    using var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
                    var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", routeRequest);
                    await ProcessRouteCalculationResult(result);
                    await ShowRouteItems(extractedCoordinates);
                    showResultsForReductionFactor = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating route reduction factor: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                isLoading = false;
                var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;
                await JS.InvokeVoidAsync("console.log", $"Total execution time: {totalTimeTaken} s");
            }
        }
        private async Task CalculateUsingVoyageLegs()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);

                // Prepare the request body matching the API expectations
                // Convert your VoyageLeg to the API's VoyageLegs format
                var apiRequest = new RouteReductionFactorsRequest
                {
                    Correction = true, // You can make this configurable
                    ExceedanceProbability = 0.00001, // You can make this configurable
                    VoyageLegs = voyageLegs.Select((leg, index) => new VoyageLegs
                    {
                        VoyageLegOrder = index + 1, // Generate order since your VoyageLeg doesn't have it
                        Coordinates = leg.Coordinates.Select(coord => new Coordinates
                        {
                            Latitude = (float)coord[1], // coord[1] is latitude in double[] format
                            Longitude = (float)coord[0] // coord[0] is longitude in double[] format
                        }).ToList()
                    }).ToList()
                };

                var result = await httpClient.PostAsJsonAsync("calculations/reduction-factors/route", apiRequest);

                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    var reductionFactorResponse = JsonSerializer.Deserialize<VoyageLegReductionFactorResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Process the response
                    await ProcessReductionFactorResponse(reductionFactorResponse);
                    showResultsForReductionFactor = true;
                }
                else
                {
                    var errorContent = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error calling reduction factor API: {result.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CalculateUsingVoyageLegs: {ex.Message}");
                throw;
            }
        }
        private async Task ProcessReductionFactorResponse(VoyageLegReductionFactorResponse response)
        {
            try
            {
                if (response?.Route?.ReductionFactors != null)
                {
                    // Store the overall route reduction factor in routeModel
                    if (routeModel != null)
                    {
                        routeModel.ReductionFactor = response.Route.ReductionFactors.Annual;
                    }

                    // Store the complete response for detailed UI display
                    this.reductionFactorResponse = response;

                    // Update route legs with reduction factors if they exist
                    if (response.VoyageLegs != null && response.VoyageLegs.Any() && routeLegs != null)
                    {
                        for (int i = 0; i < Math.Min(routeLegs.Count, response.VoyageLegs.Count); i++)
                        {
                            var apiLeg = response.VoyageLegs.FirstOrDefault(vl => vl.VoyageLegOrder == i + 1);
                            if (apiLeg != null)
                            {
                                routeLegs[i].ReductionFactor = apiLeg.ReductionFactors.Annual;
                            }
                        }
                    }

                    Console.WriteLine($"Route Reduction Factors processed successfully. Annual: {response.Route.ReductionFactors.Annual}");
                }

                // Trigger UI update
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing reduction factor response: {ex.Message}");
                throw;
            }
        }

        // 3. Helper method to get seasonal reduction factor values
        private double GetSeasonalReductionFactor(string season, bool isRoute = true, int legOrder = 0)
        {
            if (reductionFactorResponse == null) return 0.0;

            ReductionFactors factors = null;

            if (isRoute)
            {
                factors = reductionFactorResponse.Route?.ReductionFactors;
            }
            else
            {
                var leg = reductionFactorResponse.VoyageLegs?.FirstOrDefault(vl => vl.VoyageLegOrder == legOrder);
                factors = leg?.ReductionFactors;
            }

            if (factors == null) return 0.0;

            return season.ToLower() switch
            {
                "annual" => factors.Annual,
                "spring" => factors.Spring,
                "summer" => factors.Summer,
                "fall" => factors.Fall,
                "winter" => factors.Winter,
                _ => 0.0
            };
        }
        //private async Task CalculateRouteReductionFactor()
        //{
        //    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //    try
        //    {
        //        // Validate necessary data is available
        //        if (!ValidateRouteData())
        //        {
        //            // Show error message to user
        //            // You can implement this using a toast/notification system
        //            Console.WriteLine("Please complete all required route information");
        //            return;
        //        }
        //        if (extractedCoordinates != null && extractedCoordinates.Any())
        //        {
        //            await ShowRouteItems(extractedCoordinates);
        //            showResultsForReductionFactor = true;
        //        }
        //        else
        //        {
        //            // Prepare the RouteRequest object
        //            var routeRequest = PrepareRouteRequest();


        //            // Call the API
        //            using var httpClient = new HttpClient();
        //            httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
        //            // httpClient.BaseAddress = new Uri("https://localhost:7155/");
        //            var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", routeRequest);
        //            await ProcessRouteCalculationResult(result);
        //            // await ShowRouteItems();
        //            await ShowRouteItems(extractedCoordinates);
        //            // var reductionFactor = CalculateReductionFactor(routeModel.WayType, routeModel.ExceedanceProbability ?? 0, result);
        //            showResultsForReductionFactor = true;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exceptions
        //        Console.WriteLine($"Error calculating route reduction factor: {ex.Message}");
        //        // You can add additional error handling or user notification here
        //    }
        //    finally
        //    {
        //        stopwatch.Stop();
        //        isLoading = false;

        //        var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;

        //        // Log total execution time to console
        //        await JS.InvokeVoidAsync("console.log", $"Total execution time: {totalTimeTaken} s");
        //    }
        //}




        private async Task ProcessRouteCalculationResult(HttpResponseMessage response)
        {
            try
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                extractedCoordinates = new List<Coordinate>();
                // Check if the route object exists
                if (root.TryGetProperty("route", out var routeElement))
                {


                    if (routeElement.TryGetProperty("geometry", out var geometryElement) &&
                geometryElement.TryGetProperty("coordinates", out var coordinatesElement) &&
                coordinatesElement.ValueKind == JsonValueKind.Array)
                    {
                        // Process each coordinate in the array
                        foreach (var coord in coordinatesElement.EnumerateArray())
                        {
                            if (coord.ValueKind == JsonValueKind.Array && coord.GetArrayLength() >= 2)
                            {
                                // Note: GeoJSON format is [longitude, latitude]
                                var longitude = coord[0].GetDouble();
                                var latitude = coord[1].GetDouble();

                                extractedCoordinates.Add(new Coordinate
                                {
                                    Latitude = latitude,
                                    Longitude = longitude
                                });
                            }
                        }
                    }


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

        private double GetDistanceFromRouteSegment(string pointId)
        {
            var routeSeg = routeModel.RouteSegments.FirstOrDefault(x => x.StartPointId == pointId);
            if (routeSeg != null)
                return routeSeg!.Distance;

            return 0.0;

        }
        public List<Services.API.Request.Waypoint> GetRouteItemsInput()
        {
            try
            {
                int wayPointId = 1;
                List<Services.API.Request.Waypoint> listWaypoints = new List<Services.API.Request.Waypoint>();

                if (routeModel.MainDeparturePortSelection != null)
                {
                    Services.API.Request.Waypoint waypoint = new Services.API.Request.Waypoint()
                    {
                        GeoPointId = routeModel.MainDeparturePortSelection.Port.Port_Id,
                        Type = "P",
                        Distance = GetDistanceFromRouteSegment(routeModel.MainDeparturePortSelection.Port.Port_Id),//need to replace with item.Distance
                        WaypointId = wayPointId++
                    };
                    listWaypoints.Add(waypoint);
                }

                if (routeModel.DepartureItems.Count() > 0)
                {
                    foreach (var item in routeModel.DepartureItems)
                    {
                        Services.API.Request.Waypoint waypoint = new Services.API.Request.Waypoint();
                        waypoint.WaypointId = wayPointId++;
                        if (item.ItemType == "P")
                        {
                            waypoint.Distance = GetDistanceFromRouteSegment(item.Port.Port.Port_Id);
                            waypoint.GeoPointId = item.Port.Port.Port_Id;
                            waypoint.Type = "P";
                        }
                        else
                        {
                            waypoint.Distance = GetDistanceFromRouteSegment(item.Waypoint.PointId);
                            waypoint.GeoPointId = item.Waypoint.PointId;
                            waypoint.Type = "W";
                        }
                        listWaypoints.Add(waypoint);
                    }
                }

                if (routeModel.ArrivalItems.Count() > 0)
                {
                    foreach (var item in routeModel.ArrivalItems)
                    {
                        Services.API.Request.Waypoint waypoint = new Services.API.Request.Waypoint();
                        waypoint.WaypointId = wayPointId++;
                        if (item.ItemType == "P")
                        {
                            waypoint.Distance = GetDistanceFromRouteSegment(item.Port.Port.Port_Id);
                            waypoint.GeoPointId = item.Port.Port.Port_Id;
                            waypoint.Type = "P";
                        }
                        else
                        {
                            waypoint.Distance = GetDistanceFromRouteSegment(item.Waypoint.PointId);
                            waypoint.GeoPointId = item.Waypoint.PointId;
                            waypoint.Type = "W";
                        }

                        listWaypoints.Add(waypoint);
                    }
                }
                if (routeModel.MainArrivalPortSelection != null)
                {
                    Services.API.Request.Waypoint waypoint = new Services.API.Request.Waypoint()
                    {
                        GeoPointId = routeModel.MainArrivalPortSelection.Port.Port_Id,
                        Type = "P",
                        //last port doesn't have distance to next port
                        Distance = 0, //need to replace with item.Distance
                        WaypointId = wayPointId++
                    };
                    listWaypoints.Add(waypoint);
                }

                return listWaypoints;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private bool IsSimpleRouteWithoutIntermediatePoints()
        {
            // Check if we have any intermediate ports
            bool hasIntermediatePorts =
                (routeModel.DeparturePorts != null && routeModel.DeparturePorts.Count > 0) ||
                (routeModel.ArrivalPorts != null && routeModel.ArrivalPorts.Count > 0) ||
                (routeModel.DepartureItems != null && routeModel.DepartureItems.Any(i => i.ItemType != "W")) ||
                (routeModel.ArrivalItems != null && routeModel.ArrivalItems.Any(i => i.ItemType != "W"));

            // Check if we have any waypoints
            bool hasWaypoints =
                (routeModel.DepartureWaypoints != null && routeModel.DepartureWaypoints.Count > 0) ||
                (routeModel.ArrivalWaypoints != null && routeModel.ArrivalWaypoints.Count > 0) ||
                (routeModel.DepartureItems != null && routeModel.DepartureItems.Any(i => i.ItemType == "W")) ||
                (routeModel.ArrivalItems != null && routeModel.ArrivalItems.Any(i => i.ItemType == "W"));

            // It's a simple route if we have main departure and arrival ports but no intermediate points
            bool hasMainPorts =
                routeModel.MainDeparturePortSelection?.Port != null &&
                routeModel.MainArrivalPortSelection?.Port != null;

            // Return true if it's just two main ports with no intermediate points
            return hasMainPorts && !hasIntermediatePorts && !hasWaypoints;
        }
        public async Task ShowRouteItems(List<Coordinate> coordinates = null)
        {
            try
            {
                // Get overall reduction factor using provided coordinates from API if available
                if (coordinates != null && coordinates.Count > 0)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    // List<Coordinate> filteredCoordinats = RemoveDuplicateCoordinates(coordinates);
                    var rf = await GetReductionFactor(coordinates);
                    stopwatch.Stop();


                    var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;

                    // Log total execution time to console
                    await JS.InvokeVoidAsync("console.log", $"Total execution time overall reductionfactor: {totalTimeTaken} s");
                    routeModel.ReductionFactor = rf;
                }
                bool isSimpleRoute = IsSimpleRouteWithoutIntermediatePoints();
                if (isSimpleRoute)
                {
                    routeLegs.Clear();
                    return;
                }
                if (routeLegs.Count == 0 && routeModel.RouteSegments != null && routeModel.RouteSegments.Count > 0)
                {
                    // Gather all port points from route segments (skip waypoints)
                    List<RoutePointRef> portPoints = new List<RoutePointRef>();

                    // Start with departure port
                    if (routeModel.MainDeparturePortSelection?.Port != null)
                    {
                        portPoints.Add(new RoutePointRef
                        {
                            Type = "departure",
                            Latitude = routeModel.MainDeparturePortSelection.Port.Latitude,
                            Longitude = routeModel.MainDeparturePortSelection.Port.Longitude,
                            Name = routeModel.MainDeparturePortSelection.Port.Name,
                            PointId = routeModel.MainDeparturePortSelection.Port.Port_Id
                        });
                    }

                    // Add intermediate ports
                    foreach (var segment in routeModel.RouteSegments)
                    {
                        // Skip if it's a waypoint (check by name or other property)
                        if (segment.EndPointName.Contains("Waypoint"))
                            continue;

                        // Add as a port point
                        portPoints.Add(new RoutePointRef
                        {
                            Type = "port",
                            Latitude = segment.EndCoordinates[1],
                            Longitude = segment.EndCoordinates[0],
                            Name = segment.EndPointName,
                            PointId = segment.EndPointId
                        });
                    }
                    if (portPoints.Count > 2)
                    {
                        // Calculate reduction factors for port-to-port legs
                        await CalculateRouteLegReductionFactors(portPoints);

                    }
                    else
                    {
                        routeLegs.Clear();
                        return;
                    }
                }

            }
            catch (Exception ex)
            {


                Console.WriteLine($"Error in ShowRouteItems: {ex.Message}");
                throw;
            }
        }

        private async Task CalculateRouteLegReductionFactors(List<RoutePointRef> portPoints)
        {
            try
            {
                routeLegs.Clear();
                var tasks = new List<Task<RouteLegModel>>();
                var httpClient = HttpClientFactory.CreateClient("routeAPI");

                // Create tasks for each leg calculation
                for (int i = 0; i < portPoints.Count - 1; i++)
                {
                    var origin = portPoints[i];
                    var destination = portPoints[i + 1];

                    // Skip if either point is not a port type
                    if (origin.Type == "waypoint" || destination.Type == "waypoint")
                        continue;

                    // Capture loop variables to avoid closure issues
                    var currentOrigin = origin;
                    var currentDestination = destination;

                    tasks.Add(CalculateSingleLegAsync(httpClient, currentOrigin, currentDestination));
                }

                // Wait for all tasks to complete and add results to routeLegs
                var results = await Task.WhenAll(tasks);
                routeLegs.AddRange(results.Where(leg => leg != null));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating route leg reduction factors: {ex.Message}");
            }
        }
        private async Task<RouteLegModel> CalculateSingleLegAsync(HttpClient httpClient, RoutePointRef origin, RoutePointRef destination)
        {
            try
            {
                // Create route request for this leg
                var legRequest = new RouteRequest
                {
                    Origin = new double[] { origin.Longitude, origin.Latitude },
                    Destination = new double[] { destination.Longitude, destination.Latitude },
                    Restrictions = new string[] { "northwest" },
                    include_ports = false,
                    Units = "nm",
                    only_terminals = true
                };

                // Call API for this leg
                var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", legRequest);

                if (result.IsSuccessStatusCode)
                {
                    var jsonString = await result.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;

                    // Extract coordinates and calculate reduction factor
                    List<Coordinate> legCoordinates = ExtractCoordinatesFromJson(root);
                    double legDistance = ExtractDistanceFromJson(root);
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    double reductionFactor = await GetReductionFactor(legCoordinates);
                    stopwatch.Stop();

                    var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;

                    // Log total execution time to console
                    await JS.InvokeVoidAsync("console.log", $"Total execution time Middle ports: {totalTimeTaken} s");
                    return new RouteLegModel()
                    {
                        DeparturePortId = origin.PointId,
                        ArrivalPortId = destination.PointId,
                        DeparturePort = origin.Name,
                        ArrivalPort = destination.Name,
                        Distance = legDistance,
                        ReductionFactor = reductionFactor
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating leg: {ex.Message}");
                return null;
            }
        }

        private List<Coordinate> ExtractCoordinatesFromJson(JsonElement root)
        {
            var coordinates = new List<Coordinate>();

            if (root.TryGetProperty("route", out var routeElement) &&
                routeElement.TryGetProperty("geometry", out var geometryElement) &&
                geometryElement.TryGetProperty("coordinates", out var coordinatesElement) &&
                coordinatesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var coord in coordinatesElement.EnumerateArray())
                {
                    if (coord.ValueKind == JsonValueKind.Array && coord.GetArrayLength() >= 2)
                    {
                        coordinates.Add(new Coordinate
                        {
                            Longitude = coord[0].GetDouble(),
                            Latitude = coord[1].GetDouble()
                        });
                    }
                }
            }

            return coordinates;
        }

        private double ExtractDistanceFromJson(JsonElement root)
        {
            if (root.TryGetProperty("route", out var routeObj) &&
                routeObj.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("length", out var lengthElement))
            {
                return lengthElement.GetDouble();
            }
            return 0;
        }
        private List<GeoPointCoordinate> GetAllRouteOrdinates()
        {
            List<GeoPointCoordinate> lst = new List<GeoPointCoordinate>();
            if (routeModel.MainDeparturePortSelection != null)
            {
                lst.Add(new GeoPointCoordinate()
                {
                    GeoPointId = routeModel.MainDeparturePortSelection.Port.Port_Id,
                    Latitude = routeModel.MainDeparturePortSelection.Port.Latitude,
                    Longitude = routeModel.MainDeparturePortSelection.Port.Longitude
                });
            }

            if (routeModel.MainArrivalPortSelection != null)
            {
                lst.Add(new GeoPointCoordinate()
                {
                    GeoPointId = routeModel.MainArrivalPortSelection.Port.Port_Id,
                    Latitude = routeModel.MainArrivalPortSelection.Port.Latitude,
                    Longitude = routeModel.MainArrivalPortSelection.Port.Longitude
                });
            }

            if (routeModel.DepartureItems != null)
            {
                foreach (var item in routeModel.DepartureItems)
                {
                    // considering port only, need to check later
                    if (item.ItemType != "W")
                    {
                        lst.Add(new GeoPointCoordinate()
                        {
                            GeoPointId = item.Port.Port.Port_Id,
                            Latitude = item.Port.Port.Latitude,
                            Longitude = item.Port.Port.Longitude
                        });
                    }
                }
            }

            if (routeModel.ArrivalItems != null)
            {
                foreach (var item in routeModel.ArrivalItems)
                {
                    if (item.ItemType != "W")
                    {
                        lst.Add(new GeoPointCoordinate()
                        {
                            GeoPointId = item.Port.Port.Port_Id,
                            Latitude = item.Port.Port.Latitude,
                            Longitude = item.Port.Port.Longitude
                        });
                    }
                }
            }
            AllCoordinates = lst;
            return lst;
        }
        private async Task<double> GetRouteItemReductionFactor(string arrPortId, string depPortId)
        {
            try
            {
                List<Coordinate> lst = new List<Coordinate>();

                // Try to find coordinates in extractedCoordinates first
                if (extractedCoordinates != null && extractedCoordinates.Count > 0)
                {
                    // If we have the coordinates from API extraction already, use them
                    // We need to match port IDs with the closest coordinates from the extracted path

                    // Get all coordinates for the route segments
                    var routeSegments = routeModel.RouteSegments;
                    if (routeSegments != null)
                    {
                        // Find the segment that connects these ports
                        var segment = routeSegments.FirstOrDefault(s =>
                            (s.StartPointId == depPortId && s.EndPointId == arrPortId) ||
                            (s.StartPointId == arrPortId && s.EndPointId == depPortId));

                        if (segment != null)
                        {
                            // Use the start and end coordinates from the segment
                            lst.Add(new Coordinate()
                            {
                                Latitude = segment.StartCoordinates[1],
                                Longitude = segment.StartCoordinates[0]
                            });

                            lst.Add(new Coordinate()
                            {
                                Latitude = segment.EndCoordinates[1],
                                Longitude = segment.EndCoordinates[0]
                            });

                            double result = await GetReductionFactor(lst);
                            return result;
                        }
                    }
                }

                // Fallback to using AllCoordinates if available
                if (AllCoordinates != null && AllCoordinates.Any())
                {
                    // arrival location coords
                    var arrPoint = AllCoordinates.FirstOrDefault(x => x.GeoPointId == arrPortId);
                    if (arrPoint != null)
                    {
                        lst.Add(new Coordinate() { Latitude = arrPoint.Latitude, Longitude = arrPoint.Longitude });
                    }

                    // departure location coords
                    var depPoint = AllCoordinates.FirstOrDefault(x => x.GeoPointId == depPortId);
                    if (depPoint != null)
                    {
                        lst.Add(new Coordinate() { Latitude = depPoint.Latitude, Longitude = depPoint.Longitude });
                    }

                    if (lst.Count == 2)
                    {
                        double result = await GetReductionFactor(lst);
                        return result;
                    }
                }

                // If we don't have the coordinates yet, try to populate AllCoordinates
                if ((AllCoordinates == null || !AllCoordinates.Any()) && (extractedCoordinates == null || !extractedCoordinates.Any()))
                {
                    AllCoordinates = GetAllRouteOrdinates();
                    return await GetRouteItemReductionFactor(arrPortId, depPortId);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRouteItemReductionFactor: {ex.Message}");
                return 0;
            }
        }

        //private async Task<double> GetRouteItemReductionFactor(string arrPortId, string depPortId)
        //{
        //    try
        //    {
        //        List<Coordinate> lst = new List<Coordinate>();
        //        // arrival location coords
        //        double arrLat = AllCoordinates.Where(x => x.GeoPointId == arrPortId).FirstOrDefault()!.Latitude;
        //        double arrLon = AllCoordinates.Where(x => x.GeoPointId == arrPortId).FirstOrDefault()!.Longitude;
        //        lst.Add(new Coordinate() { Latitude = arrLat, Longitude = arrLon });

        //        // departure location coords
        //        double depLat = AllCoordinates.Where(x => x.GeoPointId == depPortId).FirstOrDefault()!.Latitude;
        //        double depLon = AllCoordinates.Where(x => x.GeoPointId == depPortId).FirstOrDefault()!.Longitude;
        //        lst.Add(new Coordinate() { Latitude = depLat, Longitude = depLon });

        //        double result = await GetReductionFactor(lst);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;

        //    }
        //}
        //private async Task<double> GetReductionFactor(List<Coordinate> lstCoOrdinates)
        //{
        //    double result = 0;
        //    try
        //    {
        //        var request = new RFCalculationRequest()
        //        {
        //            PointNumber = lstCoOrdinates.Count,
        //            Coordinates = lstCoOrdinates,
        //            ExceedanceProbability = routeModel.ExceedanceProbability ?? 0,
        //            WaveType = "ABS"
        //        };
        //        var response = await routeAPIService.GetReductionFactor(request);
        //        if (response != null)
        //            result = response.ReductionFactor;
        //    }
        //    catch (Exception ex)
        //    {
        //        return 0;
        //    }
        //    return result;
        //}

        private async Task<double> GetReductionFactor(List<Coordinate> lstCoOrdinates)
        {
            double result = 0;
            try
            {

                List<Coordinate> cleanedCoordinates = RemoveDuplicateCoordinates(lstCoOrdinates);

                var request = new RFCalculationRequest()
                {

                    PointNumber = cleanedCoordinates.Count,
                    Coordinates = cleanedCoordinates,
                    ExceedanceProbability = routeModel.ExceedanceProbability ?? 0,
                    WaveType = "ABS"
                };

                // Call the API with the cleaned data
                var response = await routeAPIService.GetReductionFactor(request);

                if (response != null)
                    result = response.ReductionFactor;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error calculating reduction factor: {ex.Message}");
                return 0;
            }

            return result;
        }
        private List<SeasonReductionFactor> GetSeasonReductionFactors(decimal rf)
        {
            List<SeasonReductionFactor> rfs = [];
            rfs.Add(new SeasonReductionFactor("1", Convert.ToDouble(rf)));//Annual
            rfs.Add(new SeasonReductionFactor("2", Convert.ToDouble(GetCorrectedReductionFactor(rf, "Spring"))));
            rfs.Add(new SeasonReductionFactor("3", Convert.ToDouble(GetCorrectedReductionFactor(rf, "Summer"))));
            rfs.Add(new SeasonReductionFactor("4", Convert.ToDouble(GetCorrectedReductionFactor(rf, "Fall"))));
            rfs.Add(new SeasonReductionFactor("5", Convert.ToDouble(GetCorrectedReductionFactor(rf, "Winter"))));
            return rfs;
        }
        private AddRecord GetInputtoSaveRoute()
        {
            string userId = Services.API.Endpoints.USERID;
            List<Services.API.Request.VoyageLeg> voyageLegs = [];
            List<Services.API.Request.WayPoint> wayPoints = [];
            if (routeLegs.Count > 0)
            {
                foreach (var item in routeLegs)
                {
                    _ = Guid.TryParse(item.DeparturePortId, out Guid depPortId);
                    _ = Guid.TryParse(item.ArrivalPortId, out Guid arrPortId);
                    voyageLegs.Add(new Services.API.Request.VoyageLeg(string.Empty, depPortId, arrPortId,
                         item.Distance, 1, GetSeasonReductionFactors((decimal)item.ReductionFactor)));
                }
            }

            //if (routeModel.DepartureWaypoints.Count > 0)
            //{
            //    foreach (var item in routeModel.DepartureWaypoints)
            //    {
            //        double distance = GetDistanceFromRouteSegment(item.PointId);
            //        _ = Guid.TryParse(item.PointId, out Guid pointId);
            //        wayPoints.Add(new Services.API.Request.WayPoint(0, userId, string.Empty, string.Empty, pointId, distance));
            //    }
            //}

            if (routeModel.RouteSegments.Count > 0)
            {
                for (int i = 0; i < routeModel.RouteSegments.Count; i++)
                {
                    var item = routeModel.RouteSegments[i];
                    double distance = item.Distance;
                    _ = Guid.TryParse(item.StartPointId, out Guid pointId);
                    wayPoints.Add(new Services.API.Request.WayPoint(0, i, pointId, distance));
                }
                var lastItem = routeModel.RouteSegments.LastOrDefault();
                _ = Guid.TryParse(lastItem?.EndPointId, out Guid arrPointId);
                //last point distance is always 0
                wayPoints.Add(new Services.API.Request.WayPoint(0, wayPoints.Count + 1, arrPointId, 0));
            }

            //_ = Guid.TryParse(routeModel?.MainDeparturePortSelection?.Port?.Port_Id, out Guid mainDepPortId);
            //_ = Guid.TryParse(routeModel?.MainArrivalPortSelection?.Port?.Port_Id, out Guid mainArrPortId);
            string recordId = string.Empty;
            if (!string.IsNullOrEmpty(EditRouteId))
                recordId = EditRouteId;
            var record = new AddRecord(userId, recordId, routeModel.RouteName, routeModel?.TotalDistance ?? default,
               voyageLegs, wayPoints, GetSeasonReductionFactors((decimal)routeModel?.ReductionFactor));

            return record;
        }

        private async Task AddDepartureGeoPoints()
        {
            foreach (var item in routeModel.DepartureWaypoints)
            {
                double.TryParse(item.Latitude, out double lat);
                double.TryParse(item.Longitude, out double lon);
                await routeAPIService.AddGeoPointAsync(item.PointId, lat, lon);

            }
        }

        private async Task RestoreRoute(string routeId)
        {
            try
            {
                _ports = new List<PortModel>();

                routeModel = new RouteModel();
                departureSearchResults = new List<PortModel>();
                arrivalSearchResults = new List<PortModel>();
                routeModel.DepartureItems = [];
                routeModel.DeparturePorts = [];
                routeModel.DepartureWaypoints = [];
                routeModel.ArrivalItems = [];
                routeModel.ArrivalPorts = [];
                await JS.InvokeVoidAsync("resetMap");
                routeModel.RouteId = routeId;

                var details = await routeAPIService.RestoreRouteAsync(routeId);
                routeModel.RouteName = details.RouteName;

                var mainDep = details.RoutePoints.First();
                //await SearchDepartureLocation();

                var tempPortSelection = new PortSelectionModel
                {
                    SearchTerm = mainDep.PortData.PortName,
                    SearchResults = new List<PortModel>() {new PortModel()
                    {
                        Unlocode = mainDep.PortData.PortCode,
                        Name = mainDep.PortData.PortName,
                        Latitude = mainDep.Latitude,
                        Longitude = mainDep.Longitude,
                        Port_Id = mainDep.GeoPointId,
                        Country = mainDep.PortData.CountryName
                    }}
                };
                routeModel.MainDeparturePortSelection = tempPortSelection;

                await UpdateDeparturePortSearchDepartureLocation(routeModel.MainDeparturePortSelection,
                    routeModel.MainDeparturePortSelection.SearchResults[0]);

                if (details.RoutePoints.Count > 2)
                {
                    for (int i = 1; i < details.RoutePoints.Count - 1; i++)
                    {
                        var portItem = details.RoutePoints[i];
                        if (portItem.RoutePointType == "port")
                        {
                            var portModel = new PortModel()
                            {
                                Unlocode = portItem.PortData.PortCode,
                                Name = portItem.PortData.PortName,
                                Latitude = portItem.Latitude,
                                Longitude = portItem.Longitude,
                                Port_Id = portItem.GeoPointId,
                                Country = portItem.PortData.CountryName
                            };

                            var portSelectionModel = new PortSelectionModel
                            {
                                SequenceNumber = routeModel.DepartureItems.Count + 1,
                                Port = portModel
                            };

                            // Add to combined list
                            routeModel.DepartureItems.Add(new RouteItemModel
                            {
                                SequenceNumber = routeModel.DepartureItems.Count + 1,
                                ItemType = "P",
                                Port = portSelectionModel
                            });


                            routeModel.DeparturePorts.Add(portSelectionModel);

                            await UpdateDeparturePort(portSelectionModel, portModel);

                        }
                        else
                        {
                            var waypointModel = new WaypointModel
                            {
                                SequenceNumber = routeModel.DepartureItems.Count + 1,
                                PointId = portItem.GeoPointId
                            };

                            // Add to combined list
                            routeModel.DepartureItems.Add(new RouteItemModel
                            {
                                SequenceNumber = routeModel.DepartureItems.Count + 1,
                                ItemType = "W",
                                Waypoint = waypointModel
                            });

                            // Add to waypoints list if you're still maintaining it
                            routeModel.DepartureWaypoints.Add(waypointModel);

                            await JS.InvokeVoidAsync("editWaypoint", portItem.Latitude, portItem.Longitude);
                        }
                    }
                }

                var mainArr = details.RoutePoints.Last();
                if (mainArr.PortData != null)
                {
                    //await SearchArrivalLocation();

                    var tempArrPortSelection = new PortSelectionModel
                    {
                        SearchTerm = mainArr.PortData.PortName,
                        SearchResults = new List<PortModel>() {new PortModel()
                            {
                                Unlocode = mainArr.PortData.PortCode,
                                Name = mainArr.PortData.PortName,
                                Latitude = mainArr.Latitude,
                                Longitude = mainArr.Longitude,
                                Port_Id = mainArr.GeoPointId,
                                Country = mainArr.PortData.CountryName
                            }}
                    };

                    routeModel.MainArrivalPortSelection = tempArrPortSelection;

                    await UpdateArrivalPortSearchArrivalLocation(routeModel.MainArrivalPortSelection,
                    routeModel.MainArrivalPortSelection.SearchResults[0]);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //Modified by Niranjan
        private static int FindClosestCoordinateIndex(List<double[]> coordinates, double lat, double lon)
        {
            int closestIndex = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < coordinates.Count; i++)
            {
                double dLat = coordinates[i][1] - lat;
                double dLon = coordinates[i][0] - lon;
                double dist = dLat * dLat + dLon * dLon;
                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }
            return closestIndex;
        }
        //Modified by Niranjan - Restored
        public static List<double[]> NormalizeLongitudesAndRemoveDuplicates(List<double[]> coordinates)
        {
            var seen = new HashSet<string>();
            var result = new List<double[]>();
            foreach (var coord in coordinates)
            {
                double lng = coord[0];
                double lat = coord[1];
                double lngNorm = NormalizeLongitude(lng);
                string key = $"{lngNorm:F8},{lat:F8}";
                if (!seen.Contains(key))
                {
                    result.Add(new double[] { lngNorm, lat });
                    seen.Add(key);
                }
            }
            return result;
        }
        //Modified by Niranjan - Restored
        public static double NormalizeLongitude(double longitude)
        {
            double T = 360.0;
            double t0 = -180.0;
            double k = Math.Floor((longitude - t0) / T);
            double alpha0 = longitude - k * T;
            if (alpha0 >= 180.0) alpha0 -= T;
            return alpha0;
        }
        //Modified by Niranjan
        public static List<VoyageLeg> SplitRouteIntoVoyageLegs(List<RoutePointInput> routePoints, List<double[]> fullRouteCoordinates)
        {
            var voyageLegs = new List<VoyageLeg>();
            var indices = new List<int>();
            foreach (var point in routePoints)
            {
                int idx = FindClosestCoordinateIndex(fullRouteCoordinates, point.LatLng[1], point.LatLng[0]);
                indices.Add(idx);
            }
            for (int i = 0; i < indices.Count - 1; i++)
            {
                int startIdx = indices[i];
                int endIdx = indices[i + 1];
                if (startIdx > endIdx)
                {
                    var temp = startIdx;
                    startIdx = endIdx;
                    endIdx = temp;
                }
                var legCoords = fullRouteCoordinates.GetRange(startIdx, endIdx - startIdx + 1);
                //Modified by Niranjan - Clean up coordinates
                legCoords = NormalizeLongitudesAndRemoveDuplicates(legCoords);
                var leg = new VoyageLeg
                {
                    DeparturePort = routePoints[i].Name,
                    ArrivalPort = routePoints[i + 1].Name,
                    Distance = routePoints[i + 1].SegmentDistance, // or calculate as needed
                    Coordinates = legCoords
                };
                voyageLegs.Add(leg);
            }
            return voyageLegs;
        }
        //Modified by Niranjan
    }
}