using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static System.Net.WebRequestMethods;
using SeaRouteModel.Models;

namespace SeaRouteBlazorServerApp.Components.Pages
{
    public partial class RouteManagement
    {
        //parent component
        private ElementReference reportMapContainer;
        private int selectedTab = 1;
        private int selectedForm = 0;
        private ReductionFactorCal? reductionFactorCalRef;
        private ReductionFactor reductionFactor = new ReductionFactor();
        private DotNetObjectReference<RouteManagement>? objRef;
        private void SelectTab(int tab) => selectedTab = tab;
        private void CloseOverlay() { /* Logic to hide overlay */ }
        private List<PortModel> _ports;
        private decimal? routeReductionFactor = 0.82M;
        private RouteModel routeModel = new RouteModel();
        private List<VesselInfo> vesselInfos = new List<VesselInfo>();
        private void ShowForm(int option)
        {
            selectedForm = option;
            StateHasChanged();
        }
        private bool showReport = false;
        private bool showReportForReductionFactor = false;
        private bool AddEditVessalReport = false;



        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;
            await GetSampleports();
            if (vesselInfos.Count == 0)
            {
                AddNewVessel();
            }
        }
        private void AddNewVessel()
        {
            vesselInfos.Add(new VesselInfo());
            StateHasChanged();
        }

        private void RemoveVessel(VesselInfo vessel)
        {
            if (vesselInfos.Count > 1) // Always keep at least one form
            {
                vesselInfos.Remove(vessel);
                StateHasChanged();
            }
        }
        [JSInvokable]
        public void CaptureCoordinates(double latitude, double longitude)
        {

            if (reductionFactorCalRef != null)
            {
                // Forward coordinates to child component
                reductionFactorCalRef.CaptureCoordinates(latitude, longitude);
            }
            //if (routeModel.DepartureWaypoints.Count > 0)
            //{
            //    var lastWaypoint = routeModel.DepartureWaypoints.Last();
            //    lastWaypoint.Latitude = latitude.ToString();
            //    lastWaypoint.Longitude = longitude.ToString();
            //    StateHasChanged();
            //}
        }
        private async Task HandleReportData(ReductionFactor reportData)
        {
            reductionFactor = reportData;

            StateHasChanged();
        }
        private async Task SaveVesselInfo()
        {
            // Here you can implement the save logic
            // For example, sending the vessel information to an API
            Console.WriteLine($"Saving {vesselInfos.Count} vessel information records");


        }
        private void CloseVesselInfo()
        {
            AddEditVessalReport = false;
        }
        private void CloseReport()
        {
            showReport = false;
        }
        private async Task ShowReport()
        {
            showReport = true;
            await Task.CompletedTask;
        }
        private void CloseReportForReductionFactor()
        {
            showReportForReductionFactor = false;
        }
        private async Task GoBack()
        {
            selectedForm = 0;
            AddEditVessalReport = false;
            showReport = false;
            showReportForReductionFactor = false;
            ShowABSReport = false;
        }
        private async Task AddEditVesselInfo()
        {
            AddEditVessalReport = true;
        }

        private async Task ShowReportForReductionFactor()
        {
            showReportForReductionFactor = true;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("initializeMap", objRef);
            }
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

        private async Task DownloadReport()
        {
            // In a real app, this would trigger PDF generation and download
            await JS.InvokeVoidAsync("alert", "Download report functionality would be implemented here");
        }

        private async Task PrintReport()
        {
            await JS.InvokeVoidAsync("window.print");
        }

        // report for ShowABSReport later move to separate component
        private bool ShowABSReport { get; set; } = false;
        private List<ShowABSReportForm> vesselImos = new List<ShowABSReportForm>()
    {
        new ShowABSReportForm { ImoNumber = "" },
    };
        public class ShowABSReportForm
        {
            public string ImoNumber { get; set; } = string.Empty;
        }
        private void CloseABSReportForm()
        {
            ShowABSReport = false;
        }

        private void AddNewImo()
        {
            vesselImos.Add(new ShowABSReportForm { ImoNumber = "" });
            StateHasChanged();
        }

        private void SubmitABSReport()
        {
            // Logic to submit the ABS report
            // You can add validation and submission logic here
        }
        private async Task ShowAbsReport()
        {
            ShowABSReport = true;
        }
    }
}
