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
        private List<string> seasonalOptions = new() { "Annual", "Spring", "Summer", "Fall", "Winter" };
        private void ShowForm(int option)
        {
            selectedForm = option;
            StateHasChanged();
        }
        private bool showReport = false;
        private bool showReportForReductionFactor = false;
        private bool AddEditVessalReport = false;
        private bool isCLPVChecked = false;
        private bool isCLPVParrChecked = false;

        private string HeadingText { get; set; } = "Report 1 Reduction Factor Calculation";
        private string OriginalText { get; set; } = "Report 1 Reduction Factor Calculation";
        private bool IsEditing { get; set; } = false;

        private void StartEditing()
        {
            OriginalText = HeadingText;
            IsEditing = true;
        }

        private void SaveHeading()
        {
            IsEditing = false;
            // Here you could add code to save the heading to a database or state management service
        }

        private void CancelEditing()
        {
            HeadingText = OriginalText;
            IsEditing = false;
        }
        protected async override Task OnInitializedAsync()
        {
            await Task.CompletedTask;
            await GetSampleports();
            if (vesselInfos.Count == 0)
            {
                AddNewVessel();
            }
        }
        private string GetReductionFactorForSeason(string season)
        {
            // Placeholder: Replace with real logic or data
            return season switch
            {
                "Spring" => "0.85",
                "Summer" => "0.80",
                "Fall" => "0.78",
                "Winter" => "0.75",
                _ => "0.82" // Default or "Annual"
            };
        }

        private void OnCheckboxChanged(ChangeEventArgs e, string checkboxType)
        {
            bool isChecked = (bool)e.Value;

            if (checkboxType == "CLP-V")
            {
                isCLPVChecked = isChecked;
                isCLPVParrChecked = false;
            }
            else if (checkboxType == "CLP-V(PARR)")
            {
                isCLPVParrChecked = isChecked;
                isCLPVChecked = false;
            }

            // Show report only if any one is checked
            showReportForReductionFactor = isCLPVChecked || isCLPVParrChecked;
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
        [JSInvokable]
        public async Task RecalculateRoute()
        {
            if (reductionFactorCalRef != null)
            {
                // Forward coordinates to child component
               await reductionFactorCalRef.CalculateMultiSegmentRoute();
            }
            
        }
        private async Task HandleReportData(ReductionFactor reportData)
        {
            reductionFactor = reportData;

            StateHasChanged();
        }
        private async Task HandleReductionReportData(RouteModel _routeModel)
        {
            //routeModel = _routeModel;
            routeModel = new RouteModel();
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
            try
            {
                // Create report data 
                var reportData = CreateShortVoyageReportData();

                // Generate file name
                string fileName = $"ShortVoyageReport_{DateTime.Now:yyyyMMdd}_{reductionFactor.VesselName}.pdf";

                // Call the PDF service
                await PdfService.DownloadPdfAsync(reportData, fileName);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.Error.WriteLine($"Error downloading report: {ex.Message}");
            }
        }

        private ReportData CreateShortVoyageReportData()
        {
            var reportData = new ReportData
            {
                ReportName = "Short Voyage Reduction Factor Report",
                Title = HeadingText,
                AttentionText = $"Mr. Alan Bond, Mani Industries (WCN: 123456)"
            };

            // User Inputs Section
            var userInputsSection = new ReportSection
            {
                Title = "User Inputs",
                Type = "table",
                TableData = new List<ReportTableRow>
            {
                // Header row
                new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
                // Data rows
                new ReportTableRow { Cells = new List<string> { "Vessel Name", reductionFactor.VesselName ?? "N/A" } },
                new ReportTableRow { Cells = new List<string> { "Vessel IMO", reductionFactor.IMONo ?? "N/A" } },
                new ReportTableRow { Cells = new List<string> { "Vessel Breadth", reductionFactor.Breadth.ToString() } },
                new ReportTableRow { Cells = new List<string> { "Port of Departure", reductionFactor.PortOfDeparture ?? "N/A" } },
                new ReportTableRow { Cells = new List<string> { "Port of Arrival", reductionFactor.PortOfArrival ?? "N/A" } },
                new ReportTableRow { Cells = new List<string> { "Time Zone", reductionFactor.TimeZone ?? "UTC" } },
                new ReportTableRow { Cells = new List<string> { "Date of Departure", reductionFactor.DateOfDeparture.ToString("yyyy-MM-dd") } },
                new ReportTableRow { Cells = new List<string> { "Date of Arrival", reductionFactor.DateOfArrival.ToString("yyyy-MM-dd") } },
                new ReportTableRow { Cells = new List<string> { "Duration (Hrs)", reductionFactor.Duration.ToString() } },
                new ReportTableRow { Cells = new List<string> { "ETD", reductionFactor.ETD.ToString("HH:mm") } },
                new ReportTableRow { Cells = new List<string> { "ETA", reductionFactor.ETA.ToString("HH:mm") } }
            }
            };
            reportData.Sections.Add(userInputsSection);

            // Weather Forecast Section
            var weatherSection = new ReportSection
            {
                Title = "Weather Forecast",
                Type = "table",
                TableData = new List<ReportTableRow>
            {
                new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
                new ReportTableRow { Cells = new List<string> { "Date", reductionFactor.WeatherForecastDate.ToString("yyyy-MM-dd") } },
                new ReportTableRow { Cells = new List<string> { "Time", reductionFactor.WeatherForecasetTime.ToString("HH:mm") } },
                new ReportTableRow { Cells = new List<string> { "Before ETD (Hrs)", reductionFactor.WeatherForecastBeforeETD.ToString() } },
                new ReportTableRow { Cells = new List<string> { "Source", reductionFactor.WeatherForecastSource ?? "www.weather.gov" } }
            }
            };
            reportData.Sections.Add(weatherSection);

            // Wave Height Section
            //var waveHeightSection = new ReportSection
            //{
            //    Title = "Forecast Maximum Significant Wave Height",
            //    Type = "table",
            //    TableData = new List<ReportTableRow>
            //{
            //    new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
            //    new ReportTableRow { Cells = new List<string> { "Hswell [m]", reductionFactor.WaveHeightHswell.ToString() } },
            //    new ReportTableRow { Cells = new List<string> { "Hwind [m]", reductionFactor.WaveHeightHwind.ToString() } },
            //    new ReportTableRow { Cells = new List<string> { "Hs, max [m]", reductionFactor.WaveHsmax.ToString() } }
            //}
            //};
            //reportData.Sections.Add(waveHeightSection);

            // Output Section
            var outputSection = new ReportSection
            {
                Title = "Output",
                Type = "text",
                Content = $"Duration OK: {reductionFactor.DurationOk}\n" +
                          $"Forecast Before ETD OK: {reductionFactor.WeatherForecastBeforeETDOK}\n" +
                          $"Reduction Factor: {reductionFactor.ShortVoyageReductionFactor?.ToString("0.00") ?? "N/A"}"
            };
            reportData.Sections.Add(outputSection);

            // Add notes
            reportData.Notes = new List<string>
        {
            "The vessel is to have CLP-V or CLP-VP(XR) notation, and the onboard Computer Lashing Program is to be approved to handle Short Voyage Reduction Factors.",
            "The minimum value of the Short Voyage Reduction Factor is 0.6 and needs to be included in Cargo Securing Manual (CSM).",
            "A short voyage is to have a duration of less than 72 hours from departure port to arrival port.",
            "The weather reports need to be received within 6 hours of departure.",
            "The forecasted wave height needs to cover the duration of the voyage plus 12 hours."
        };

            return reportData;
        }

        //For reduction factor
        private async Task DownloadReductionFactorReport()
        {
            try
            {
                // Create report data 
                var reportData = CreateReductionFactorReportData();

                // Generate file name
                string fileName = $"ReductionFactorReport_{DateTime.Now:yyyyMMdd}_{routeModel?.RouteName?.Replace(" ", "_") ?? "Route"}.pdf";

                // Call the PDF service
                await PdfService.DownloadPdfAsync(reportData, fileName);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.Error.WriteLine($"Error downloading reduction factor report: {ex.Message}");
            }
        }

        private ReportData CreateReductionFactorReportData()
        {
            var reportData = new ReportData
            {
                ReportName = "Route Reduction Factor Report",
                Title = HeadingText,
                AttentionText = $"Mr. Alan Bond, Mani Industries (WCN: 123456)"
            };

            // User Inputs Section
            var userInputsSection = new ReportSection
            {
                Title = "User Inputs",
                Type = "table",
                TableData = new List<ReportTableRow>
        {
            // Header row
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value", "Code" } },
            // Data rows
            new ReportTableRow { Cells = new List<string> { "Route Name", routeModel?.RouteName ?? "Marseille - Shanghai", "" } },
            new ReportTableRow { Cells = new List<string> { "Port of Departure", routeModel?.MainDeparturePortSelection?.Port?.Name ?? "Marseille, France", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Unlocode ?? "FRMRS" } },
            new ReportTableRow { Cells = new List<string> { "Loading Port", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Name ?? "Singapore", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Unlocode ?? "SGSIN" } },
            new ReportTableRow { Cells = new List<string> { "Port of Arrival", routeModel?.MainArrivalPortSelection?.Port?.Name ?? "Shanghai, China", routeModel?.ArrivalPorts?.FirstOrDefault()?.Port?.Unlocode ?? "CNSGH" } }
        }
            };
            reportData.Sections.Add(userInputsSection);

            // Output Section
            var outputSection = new ReportSection
            {
                Title = "Output",
                Type = "table"
            };

            if (isCLPVChecked)
            {
                // Single reduction factor for CLP-V
                outputSection.TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
            new ReportTableRow { Cells = new List<string> { "Reduction Factor", routeReductionFactor?.ToString("0.00") ?? "0.82" } }
        };
            }
            else if (isCLPVParrChecked)
            {
                // Seasonal reduction factors for CLP-VP(XR)
                outputSection.TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Season", "Reduction Factor" } }
        };

                foreach (var season in seasonalOptions)
                {
                    outputSection.TableData.Add(new ReportTableRow
                    {
                        Cells = new List<string> { season, GetReductionFactorForSeason(season) }
                    });
                }
            }

            reportData.Sections.Add(outputSection);

            // Add notes
            reportData.Notes = new List<string>
    {
        "The vessel is to have CLP-V or CLP-VP(XR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
        "ABS Container Securing Guide 6.2.4"
    };

            return reportData;
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
