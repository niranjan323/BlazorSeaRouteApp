using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using NextGenEngApps.DigitalRules.CRoute.Data;
using NextGenEngApps.DigitalRules.CRoute.Models;
using NextGenEngApps.DigitalRules.CRoute.Services.API.Request;
using SeaRouteModel.Models;
using System.Diagnostics;
using System.Text.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static SeaRouteModel.Models.ReductionFactor;
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
        public EventCallback<RouteModel> OnShowAbsReport { get; set; }
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

        [Parameter]
        public EventCallback<VoyageLegReductionFactorResponse> OnReductionFactorDataReceived { get; set; }

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
        private bool isMapLoading = true;
        private string errorMessage = string.Empty;
        private string saveErrorMessage = string.Empty;
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
        private RouteModel model = new();
        private string departureSearchTerm = string.Empty;
        private string arrivalSearchTerm = string.Empty;
        private List<PortModel> departureSearchResults = new List<PortModel>();
        private List<PortModel> arrivalSearchResults = new List<PortModel>();
        private bool isSeasonFixed = false;
        private List<RouteLegModel> routeLegs = new List<RouteLegModel>();
        private List<GeoPointCoordinate> AllCoordinates = new List<GeoPointCoordinate>();
        private List<Coordinate> extractedCoordinates = new List<Coordinate>();
        // Add a field to store the split voyage legs
        private List<VoyageLeg> _voyageLegs = new List<VoyageLeg>();
        private VoyageLegReductionFactorResponse reductionFactorResponse;

        private static readonly string CalculateSearouteEndpoint = "api/v1/searoutes/calculate-route";
        private static readonly string CalculateRouteReductionFactorEndpoint = "calculations/reduction-factors/route";

        protected async override Task OnInitializedAsync()
        {
            routeModel.ReportDate = DateTime.Now;

            if (!string.IsNullOrEmpty(EditRouteId))
                await RestoreRoute(EditRouteId);

            await Task.CompletedTask;
            await GetSampleports();
            model = CloneModel(routeModel);
        }
        private RouteModel CloneModel(RouteModel source)
        {
            return new RouteModel
            {
                RouteName = source.RouteName,
                Vessel = new VesselInfo()
                {
                    VesselName = source.Vessel.VesselName,
                    IMONumber = source.Vessel.IMONumber,
                    Flag = source.Vessel.Flag,
                    Breadth = source.Vessel.Breadth,
                },
                MainDeparturePortSelection = new PortSelectionModel()
                {
                    SearchTerm = source.MainDeparturePortSelection.SearchTerm,
                },
                MainArrivalPortSelection = new PortSelectionModel()
                {
                    SearchTerm = source.MainArrivalPortSelection.SearchTerm,
                },
            };
        }
        public bool HasUnsavedChanges()
        {
            return (routeModel?.RouteName ?? "") != (model?.RouteName ?? "") ||
                   (routeModel?.Vessel?.VesselName ?? "") != (model?.Vessel?.VesselName ?? "") ||
                   (routeModel?.Vessel?.IMONumber ?? "") != (model?.Vessel?.IMONumber ?? "") ||
                   (routeModel?.Vessel?.Flag ?? "") != (model?.Vessel?.Flag ?? "") ||
                   (routeModel?.Vessel?.Breadth ?? 0) != (model?.Vessel?.Breadth ?? 0) ||
                   (routeModel?.MainArrivalPortSelection?.Port) != (model?.MainArrivalPortSelection?.Port) ||
                   (routeModel?.MainDeparturePortSelection?.Port) != (model?.MainDeparturePortSelection?.Port);
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


        public async Task EnableCalculateButton()
        {
            isMapLoading = false;
            StateHasChanged();
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
            var portModel = new PortSelectionModel { SequenceNumber = 1 };
            var newRouteItem = new RouteItemModel
            {
                SequenceNumber = 1,
                ItemType = "P",
                Port = portModel
            };

            routeModel.DepartureItems.Insert(0, newRouteItem);
            routeModel.DeparturePorts.Add(portModel);

            ResequenceDepartureItems();

            StateHasChanged();
        }

        private void AddDeparturePortAfter(int afterSequenceNumber)
        {
            int insertIndex = routeModel.DepartureItems.FindIndex(x => x.SequenceNumber == afterSequenceNumber) + 1;

            var portModel = new PortSelectionModel { SequenceNumber = afterSequenceNumber + 1 };
            var newRouteItem = new RouteItemModel
            {
                SequenceNumber = afterSequenceNumber + 1,
                ItemType = "P",
                Port = portModel
            };
            routeModel.DepartureItems.Insert(insertIndex, newRouteItem);
            routeModel.DeparturePorts.Add(portModel);
            ResequenceDepartureItems();

            StateHasChanged();
        }


        private async Task AddDepartureWaypointAfter(int afterSequenceNumber)
        {
            int insertIndex = routeModel.DepartureItems.FindIndex(x => x.SequenceNumber == afterSequenceNumber) + 1;

            var waypointModel = new WaypointModel
            {
                SequenceNumber = afterSequenceNumber + 1,
                PointId = Guid.NewGuid().ToString()
            };

            var newRouteItem = new RouteItemModel
            {
                SequenceNumber = afterSequenceNumber + 1,
                ItemType = "W",
                Waypoint = waypointModel
            };

            routeModel.DepartureItems.Insert(insertIndex, newRouteItem);
            routeModel.DepartureWaypoints.Add(waypointModel);
            ResequenceDepartureItems();

            await EnableWaypointSelection();
            StateHasChanged();
        }

        private void ResequenceDepartureItems()
        {
            for (int i = 0; i < routeModel.DepartureItems.Count; i++)
            {
                var item = routeModel.DepartureItems[i];
                item.SequenceNumber = i + 1;
                if (item.ItemType == "P" && item.Port != null)
                {
                    item.Port.SequenceNumber = i + 1;
                }
                else if (item.ItemType == "W" && item.Waypoint != null)
                {
                    item.Waypoint.SequenceNumber = i + 1;
                }
            }
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
                var removedSequenceNumber = itemToRemove.SequenceNumber;

                routeModel.DepartureItems.Remove(itemToRemove);
                foreach (var item in routeModel.DepartureItems.Where(x => x.SequenceNumber > removedSequenceNumber))
                {
                    item.SequenceNumber--;
                    if (item.ItemType == "P" && item.Port != null)
                    {
                        item.Port.SequenceNumber = item.SequenceNumber;
                    }
                    else if (item.ItemType == "W" && item.Waypoint != null)
                    {
                        item.Waypoint.SequenceNumber = item.SequenceNumber;
                    }
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

            var sequenceNumber = portSelection.SequenceNumber;

            if (!string.IsNullOrWhiteSpace(portSelection.SearchTerm))
            {
                await JS.InvokeVoidAsync("zoomAndPinLocation", portSelection.SearchTerm, true, newPort.Latitude,
                    newPort.Longitude, sequenceNumber);
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
        public async Task SaveRouteBeforeDownload()
        {
            try
            {
                await SaveRoute();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SubmitReportSave()
        {
            try
            {
                await SaveRoute();
            }
            catch (Exception)
            {
                throw;
            }
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
            if (string.IsNullOrEmpty(EditRouteId) && !isRouteSaved)
            {
                saveErrorMessage = "Please save the route before submitting to ABS";
                return;
            }

            if (OnShowAbsReport.HasDelegate)
            {
                routeModel.IsSaveRoute = true;
                await OnShowAbsReport.InvokeAsync(routeModel);
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
            try
            {
                if (!ValidateRoute())
                    return;

                saveErrorMessage = string.Empty;
                isRouteSaved = false;
                await AddDepartureGeoPoints();
                var addRecord = await GetInputToSaveRoute();
                string result = await routeAPIService.SaveRouteAsync(addRecord);
                if (!string.IsNullOrEmpty(result))
                {
                    routeModel.RouteId = result;
                    isRouteSaved = true;
                    StateHasChanged();   // Update the UI
                }
                //hide message
                await Task.Delay(3000);
                isRouteSaved = false;
                StateHasChanged();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool ValidateRoute()
        {
            if (string.IsNullOrEmpty(routeModel.RouteName))
            {
                saveErrorMessage = "Enter route name";
                return false;
            }
            if (string.IsNullOrEmpty(routeModel.Vessel.VesselName))
            {
                saveErrorMessage = "Enter vessel name";
                return false;
            }
            if (string.IsNullOrEmpty(routeModel.Vessel.IMONumber))
            {
                saveErrorMessage = "Enter vessel imo number";
                return false;
            }
            if (string.IsNullOrEmpty(routeModel.Vessel.Flag))
            {
                saveErrorMessage = "Enter vessel flag";
                return false;
            }
            if (routeModel.ReportDate == null)
            {
                saveErrorMessage = "Enter report date";
                return false;
            }
            return true;
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
                SequenceNumber = 1,
                PointId = Guid.NewGuid().ToString()
            };

            var newRouteItem = new RouteItemModel
            {
                SequenceNumber = 1,
                ItemType = "W",
                Waypoint = waypointModel
            };

            routeModel.DepartureItems.Insert(0, newRouteItem);
            routeModel.DepartureWaypoints.Add(waypointModel);
            ResequenceDepartureItems();

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

        //Start
        private bool shouldCalculateVoyageLegs = false;
        private List<RoutePointInput> cachedRoutePointInputs;

        public async Task CalculateMultiSegmentRoute()
        {
            try
            {
                isMapLoading = true;
                await JS.InvokeVoidAsync("initializeRouteCalculation");
                var routePoints = await JS.InvokeAsync<List<RoutePointModel>>("getRouteData");

                if (routePoints == null || routePoints.Count < 2)
                    return;

                Dictionary<string, RoutePointRef> pointMapping = new();
                List<RouteSegmentInfo> routeSegments = new();
                double totalDistance = 0;
                double totalDuration = 0;

                // Build point mapping (ports and waypoints)
                BuildPointMapping(pointMapping);

                // Build ordered points from departure items
                List<RoutePointRef> orderedPoints = BuildOrderedPoints(pointMapping);

                bool hasDeparture = orderedPoints.Count > 0 &&
                                    (orderedPoints[0].Type == "departure" || routePoints[0].Type == "departure");

                List<List<Coordinate>> allSegmentCoordinates = new();
                double referenceLongitude = 0;

                // Calculate route segments
                if (hasDeparture && orderedPoints.Count >= 2)
                {
                    for (int i = 0; i < orderedPoints.Count - 1; i++)
                    {
                        var origin = orderedPoints[i];
                        var destination = orderedPoints[i + 1];

                        var segmentInfo = await CalculateRouteSegment(origin, destination, i);

                        if (segmentInfo != null)
                        {
                            var transformedCoordinates = (i == 0)
                                ? segmentInfo.Coordinates
                                : CoordinateUtils.TransformSegmentCoordinates(segmentInfo.Coordinates, referenceLongitude);

                            if (transformedCoordinates.Count > 0)
                            {
                                referenceLongitude = transformedCoordinates.Last().Longitude;
                            }

                            allSegmentCoordinates.Add(transformedCoordinates);

                            totalDistance += segmentInfo.Distance;
                            totalDuration += segmentInfo.DurationHours;

                            routeSegments.Add(segmentInfo);

                            // Visualize segment on map
                            await VisualizeRouteSegment(transformedCoordinates, i, orderedPoints.Count - 1);
                        }
                    }
                }

                // Update route model with segment data
                routeModel.RouteSegments = routeSegments;
                routeModel.TotalDistance = totalDistance;
                routeModel.TotalDurationHours = totalDuration;

                // Build route point inputs and cache them
                cachedRoutePointInputs = BuildRoutePointInputs(orderedPoints, allSegmentCoordinates, routeSegments);

                // ONLY calculate voyage legs if explicitly requested
                if (shouldCalculateVoyageLegs)
                {
                    await CalculateAndUpdateVoyageLegs();
                    shouldCalculateVoyageLegs = false; // Reset flag
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating route: {ex.Message}");
            }
        }



        private void BuildPointMapping(Dictionary<string, RoutePointRef> pointMapping)
        {
            // Main departure port
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

            // Main arrival port
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

            // Departure ports
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

            // Arrival ports
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

            // Departure waypoints
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
                }
            }

            // Arrival waypoints
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
        }


        private List<RoutePointRef> BuildOrderedPoints(Dictionary<string, RoutePointRef> pointMapping)
        {
            List<RoutePointRef> orderedPoints = new();

            foreach (var item in routeModel.DepartureItems.OrderBy(x => x.SequenceNumber))
            {
                if (item.ItemType == "P" && item.Port?.Port != null)
                {
                    var matchingPort = pointMapping
                        .Where(kvp => kvp.Value.Type == "port" &&
                                      kvp.Value.PointId == item.Port.Port.Port_Id)
                        .Select(kvp => kvp.Value)
                        .FirstOrDefault();

                    if (matchingPort != null)
                        orderedPoints.Add(matchingPort);
                }
                else if (item.ItemType == "W" && item.Waypoint != null)
                {
                    var matchingWaypoint = pointMapping
                        .Where(kvp => kvp.Value.Type == "waypoint" &&
                                      kvp.Value.PointId == item.Waypoint.PointId)
                        .Select(kvp => kvp.Value)
                        .FirstOrDefault();

                    if (matchingWaypoint != null)
                        orderedPoints.Add(matchingWaypoint);
                }
            }

            return orderedPoints;
        }

        private async Task<RouteSegmentInfo> CalculateRouteSegment(RoutePointRef origin, RoutePointRef destination, int segmentIndex)
        {
            var segmentRequest = new RouteRequest
            {
                Origin = new[] { origin.Longitude, origin.Latitude },
                Destination = new[] { destination.Longitude, destination.Latitude },
                Restrictions = new[] { "northwest" },
                include_ports = false,
                Units = "nm",
                only_terminals = true
            };

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
            var result = await httpClient.PostAsJsonAsync(CalculateSearouteEndpoint, segmentRequest);

            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error calculating segment {segmentIndex}: {result.StatusCode}");
                return null;
            }

            var jsonString = await result.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("route", out var routeElement) ||
                !routeElement.TryGetProperty("properties", out var propertiesElement))
            {
                return null;
            }

            List<Coordinate> coordinates = new();
            if (routeElement.TryGetProperty("geometry", out var geometryElement) &&
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

            double distance = propertiesElement.TryGetProperty("length", out var lengthElement)
                ? lengthElement.GetDouble() : 0;

            double duration = propertiesElement.TryGetProperty("duration_hours", out var durationElement)
                ? durationElement.GetDouble() : 0;

            string units = propertiesElement.TryGetProperty("units", out var unitsElement)
                ? unitsElement.GetString() : "nm";

            return new RouteSegmentInfo
            {
                SegmentIndex = segmentIndex,
                StartPointName = origin.Name,
                EndPointName = destination.Name,
                Distance = distance,
                DurationHours = duration,
                Units = units,
                StartCoordinates = new[] { origin.Longitude, origin.Latitude },
                EndCoordinates = new[] { destination.Longitude, destination.Latitude },
                StartPointId = origin.PointId,
                EndPointId = destination.PointId,
                Coordinates = coordinates
            };
        }

        private async Task VisualizeRouteSegment(List<Coordinate> coordinates, int segmentIndex, int totalSegments)
        {
            var segmentGeoJson = new
            {
                type = "Feature",
                properties = new { },
                geometry = new
                {
                    type = "LineString",
                    coordinates = coordinates.Select(c => new[] { c.Longitude, c.Latitude })
                }
            };

            var segmentJson = JsonSerializer.Serialize(segmentGeoJson);
            await JS.InvokeVoidAsync("processRouteSegment", segmentJson, segmentIndex, totalSegments);
        }


        private async Task CalculateAndUpdateVoyageLegs()
        {
            if (cachedRoutePointInputs == null || cachedRoutePointInputs.Count == 0)
            {
                Console.WriteLine("No route points available for voyage leg calculation");
                return;
            }

            _voyageLegs = SplitRouteIntoVoyageLegs(cachedRoutePointInputs);
            routeLegs.Clear();

            if (_voyageLegs?.Count > 0)
            {
                foreach (var leg in _voyageLegs)
                {
                    routeLegs.Add(new RouteLegModel
                    {
                        DeparturePort = leg.DeparturePort,
                        DeparturePortId = leg.DeparturePortId,
                        ArrivalPort = leg.ArrivalPort,
                        ArrivalPortId = leg.ArrivalPortId,
                        Distance = leg.Distance
                    });
                }
            }

            await Task.CompletedTask;
        }

        private List<RoutePointInput> BuildRoutePointInputs(
    List<RoutePointRef> orderedPoints,
    List<List<Coordinate>> allSegmentCoordinates,
    List<RouteSegmentInfo> routeSegments)
        {
            var routePointInputs = new List<RoutePointInput>();

            for (int i = 0; i < orderedPoints.Count; i++)
            {
                var point = orderedPoints[i];
                double segmentDistance = 0;
                List<double[]> segmentCoordinates = new();

                if (i < allSegmentCoordinates.Count)
                {
                    segmentCoordinates = allSegmentCoordinates[i]
                        .Select(c => new[] { c.Longitude, c.Latitude })
                        .ToList();
                }

                if (i < routeSegments.Count)
                {
                    segmentDistance = routeSegments[i].Distance;
                }

                routePointInputs.Add(new RoutePointInput
                {
                    Type = point.Type == "departure" ? "port" : point.Type,
                    Name = point.Name,
                    LatLng = new[] { point.Latitude, point.Longitude },
                    SegmentDistance = segmentDistance,
                    SegmentCoordinates = segmentCoordinates,
                    PointId = point.PointId
                });
            }

            return routePointInputs;
        }

        //END
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
                showResultsForReductionFactor = false;
                StateHasChanged();
                if (!ValidateRouteData())
                {

                    Console.WriteLine("Please complete all required route information");
                    isLoading = false;
                    return;
                }
                //Sireesha 
                shouldCalculateVoyageLegs = true;
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
                if (!ValidateRouteData())
                {
                    Console.WriteLine("Please complete all required route information");
                    return;
                }


                if (_voyageLegs != null && _voyageLegs.Count > 0)
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

                    var routeRequest = PrepareRouteRequest();
                    using var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
                    var result = await httpClient.PostAsJsonAsync(CalculateSearouteEndpoint, routeRequest);
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
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                var apiRequest = new RouteReductionFactorsRequest
                {
                    Correction = true,
                    ExceedanceProbability = 0.00001,
                    VoyageLegs = _voyageLegs.Select((leg, index) => new VoyageLegs
                    {
                        VoyageLegOrder = index + 1,
                        Coordinates = leg.Coordinates.Select(coord => new Coordinates
                        {
                            Latitude = (float)coord[1],
                            Longitude = (float)coord[0]
                        }).ToList()
                    }).ToList()
                };

                var result = await httpClient.PostAsJsonAsync(CalculateRouteReductionFactorEndpoint, apiRequest);

                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();
                    var reductionFactorResponse = JsonSerializer.Deserialize<VoyageLegReductionFactorResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

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

                    if (routeModel != null)
                    {
                        routeModel.ReductionFactor = response.Route.ReductionFactors.Annual;
                    }

                    this.reductionFactorResponse = response;


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
                    await OnReductionFactorDataReceived.InvokeAsync(response);
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
        private async Task ProcessRouteCalculationResult(HttpResponseMessage response)
        {
            try
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                extractedCoordinates = new List<Coordinate>();

                if (root.TryGetProperty("route", out var routeElement))
                {


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
                var result = await httpClient.PostAsJsonAsync(CalculateSearouteEndpoint, legRequest);

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

        private List<SeasonReductionFactor> GetSeasonReductionFactorsFromApi(bool isRoute = true, int legOrder = 0)
        {
            List<SeasonReductionFactor> rfs = [];

            if (reductionFactorResponse == null)
            {

                rfs.Add(new SeasonReductionFactor("1", 0.0));
                rfs.Add(new SeasonReductionFactor("2", 0.0));
                rfs.Add(new SeasonReductionFactor("3", 0.0));
                rfs.Add(new SeasonReductionFactor("4", 0.0));
                rfs.Add(new SeasonReductionFactor("5", 0.0));
                return rfs;
            }

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

            if (factors != null)
            {
                rfs.Add(new SeasonReductionFactor("1", factors.Annual));
                rfs.Add(new SeasonReductionFactor("2", factors.Spring));
                rfs.Add(new SeasonReductionFactor("3", factors.Summer));
                rfs.Add(new SeasonReductionFactor("4", factors.Fall));
                rfs.Add(new SeasonReductionFactor("5", factors.Winter));
            }
            else
            {

                rfs.Add(new SeasonReductionFactor("1", 0.0));
                rfs.Add(new SeasonReductionFactor("2", 0.0));
                rfs.Add(new SeasonReductionFactor("3", 0.0));
                rfs.Add(new SeasonReductionFactor("4", 0.0));
                rfs.Add(new SeasonReductionFactor("5", 0.0));
            }

            return rfs;
        }
        private async Task<AddRecord> GetInputToSaveRoute()
        {
            string userId = await authService.GetIdentityUserIdAsync();
            bool isAbsUser = true; // todo for non abs user
            List<Services.API.Request.VoyageLeg> voyageLegs = [];
            List<Services.API.Request.WayPoint> wayPoints = [];

            if (routeLegs.Count > 0)
            {
                foreach (var item in routeLegs)
                {
                    _ = Guid.TryParse(item.DeparturePortId, out Guid depPortId);
                    _ = Guid.TryParse(item.ArrivalPortId, out Guid arrPortId);

                    var voyageLegOrder = routeLegs.IndexOf(item) + 1;

                    voyageLegs.Add(new Services.API.Request.VoyageLeg(
                        string.Empty,
                        depPortId,
                        arrPortId,
                        item.Distance,
                        1,
                        GetSeasonReductionFactorsFromApi(false, voyageLegOrder)
                    ));
                }
            }

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
                // last point distance is always 0
                wayPoints.Add(new Services.API.Request.WayPoint(0, wayPoints.Count + 1, arrPointId, 0));
            }

            string recordId = string.Empty;
            if (!string.IsNullOrEmpty(EditRouteId))
                recordId = EditRouteId;
            else if (!string.IsNullOrEmpty(routeModel.RouteId))
                recordId = routeModel.RouteId;

            VesselInfo? vessel = null;
            if (!string.IsNullOrEmpty(routeModel.Vessel.VesselName)
                || !string.IsNullOrEmpty(routeModel.Vessel.IMONumber)
                || !string.IsNullOrEmpty(routeModel.Vessel.Flag))
            {
                vessel = new VesselInfo()
                {
                    VesselName = routeModel.Vessel.VesselName,
                    IMONumber = routeModel.Vessel.IMONumber,
                    Flag = routeModel.Vessel.Flag
                };

                //bind vessel to routeModel to display on show report
                routeModel.Vessel = vessel;
            }

            var record = new AddRecord(
                userId,
                recordId,
                routeModel.RouteName,
                routeModel?.TotalDistance ?? default,
                voyageLegs,
                wayPoints,
                GetSeasonReductionFactorsFromApi(true, 0),
                vessel,
                isAbsUser,
                routeModel!.ReportDate
            );

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
                isMapLoading = true;
                StateHasChanged();

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
                routeModel.Vessel = details.Vessel;
                routeModel.ReportDate = details.RecordDate;
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

        /// <summary>
        /// Create a list of voyage legs based on a list of route points.
        /// </summary>
        /// <param name="routePoints">
        ///     A list of route points. Assume that:
        ///     * There are at least two route points, and
        ///     * The first and last route points are ports.
        ///     </param>
        /// <returns>A list of voyage legs</returns>
        public static List<VoyageLeg> SplitRouteIntoVoyageLegs(List<RoutePointInput> routePoints)
        {
            VoyageLeg firstLeg = CreateVoyageLegFromRoutePointInput(routePoints[0]);
            List<VoyageLeg> voyageLegs = [];
            voyageLegs.Add(firstLeg);

            const int routePointStartingIndex = 1; // Start from the second point since the first is already added
            for (int i = routePointStartingIndex; i < routePoints.Count; i++)
            {
                VoyageLeg currentLeg = voyageLegs.Last();

                var point = routePoints[i];
                if (point.Type.ToLower() != "waypoint")
                {
                    // Conclusion of the current leg
                    currentLeg.ArrivalPort = point.Name;
                    currentLeg.ArrivalPortId = point.PointId;
                    currentLeg.Coordinates = NormalizeLongitudesAndRemoveDuplicates(currentLeg.Coordinates);

                    if (i < routePoints.Count - 1)
                    {
                        var newLeg = CreateVoyageLegFromRoutePointInput(point);
                        voyageLegs.Add(newLeg);
                    }
                }
                else
                {
                    // Continuation of the current leg
                    currentLeg.Distance += point.SegmentDistance;
                    if (point.SegmentCoordinates != null)
                        currentLeg.Coordinates.AddRange(point.SegmentCoordinates);
                }
            }

            return voyageLegs;
        }

        private static VoyageLeg CreateVoyageLegFromRoutePointInput(RoutePointInput point)
        {
            VoyageLeg voyageLeg = new()
            {
                DeparturePort = point.Name,
                DeparturePortId = point.PointId,
                Distance = point.SegmentDistance,
                Coordinates = []
            };

            if (point.SegmentCoordinates != null)
            {
                voyageLeg.Coordinates.AddRange(point.SegmentCoordinates);
            }

            return voyageLeg;
        }

        public static List<double[]> NormalizeLongitudesAndRemoveDuplicates(List<double[]> coordinates)
        {
            var result = new List<double[]>();
            string previousKey = "";

            foreach (var coord in coordinates)
            {
                double lng = coord[0];
                double lat = coord[1];
                double lngNorm = NormalizeLongitude(lng);
                string currentKey = $"{lngNorm:F8},{lat:F8}";

                if (currentKey != previousKey)
                {
                    result.Add([lngNorm, lat]);
                    previousKey = currentKey;
                }
            }

            return result;
        }

        public static double NormalizeLongitude(double longitude)
        {
            double T = 360.0;
            double t0 = -180.0;
            double k = Math.Floor((longitude - t0) / T);
            double alpha0 = longitude - k * T;
            if (alpha0 >= 180.0) alpha0 -= T;
            return alpha0;
        }
    }
}