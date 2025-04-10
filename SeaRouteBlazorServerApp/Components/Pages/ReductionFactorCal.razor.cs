﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SeaRouteModel.Models;
using static System.Net.WebRequestMethods;

namespace SeaRouteBlazorServerApp.Components.Pages
{
    public partial class ReductionFactorCal
    {
        [Parameter]
        public EventCallback OnBack { get; set; }
        [Parameter]
        public EventCallback OnAddEditVessel { get; set; }
        [Parameter] 
        public EventCallback OnShowReportForReductionFactor { get; set; }
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
        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;
            await GetSampleports();

        }
        private void CloseVesselInfo()
        {
            AddEditVessalReport = false;
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
        [JSInvokable]
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
        private RouteModel routeModel = new RouteModel();
        private string departureSearchTerm = string.Empty;
        private string arrivalSearchTerm = string.Empty;
        private List<PortModel> departureSearchResults = new List<PortModel>();
        private List<PortModel> arrivalSearchResults = new List<PortModel>();



       
       
       
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


        private void UpdateDeparturePort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            portSelection.SearchResults.Clear();
        }

        private void UpdateArrivalPort(PortSelectionModel portSelection, PortModel newPort)
        {
            portSelection.Port = newPort;
            portSelection.SearchTerm = newPort.Name;
            portSelection.SearchResults.Clear();
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

