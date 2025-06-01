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

    //    private async Task DownloadReport()
    //    {
    //        try
    //        {
    //            // Create report data 
    //            var reportData = CreateShortVoyageReportData();

    //            // Generate file name
    //            string fileName = $"ShortVoyageReport_{DateTime.Now:yyyyMMdd}_{reductionFactor.VesselName}.pdf";

    //            // Call the PDF service
    //            await PdfService.DownloadPdfAsync(reportData, fileName);
    //        }
    //        catch (Exception ex)
    //        {
    //            // Handle exception
    //            Console.Error.WriteLine($"Error downloading report: {ex.Message}");
    //        }
    //    }

    //    private ReportData CreateShortVoyageReportData()
    //    {
    //        var reportData = new ReportData
    //        {
    //            ReportName = "Short Voyage Reduction Factor Report",
    //            Title = HeadingText,
    //            AttentionText = $"Mr. Alan Bond, Mani Industries (WCN: 123456)"
    //        };

    //        // User Inputs Section
    //        var userInputsSection = new ReportSection
    //        {
    //            Title = "User Inputs",
    //            Type = "table",
    //            TableData = new List<ReportTableRow>
    //        {
    //            // Header row
    //            new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
    //            // Data rows
    //            new ReportTableRow { Cells = new List<string> { "Vessel Name", reductionFactor.VesselName ?? "N/A" } },
    //            new ReportTableRow { Cells = new List<string> { "Vessel IMO", reductionFactor.IMONo ?? "N/A" } },
    //            new ReportTableRow { Cells = new List<string> { "Vessel Breadth", reductionFactor.Breadth.ToString() } },
    //            new ReportTableRow { Cells = new List<string> { "Port of Departure", reductionFactor.PortOfDeparture ?? "N/A" } },
    //            new ReportTableRow { Cells = new List<string> { "Port of Arrival", reductionFactor.PortOfArrival ?? "N/A" } },
    //            new ReportTableRow { Cells = new List<string> { "Time Zone", reductionFactor.TimeZone ?? "UTC" } },
    //            new ReportTableRow { Cells = new List<string> { "Date of Departure", reductionFactor.DateOfDeparture.ToString("yyyy-MM-dd") } },
    //            new ReportTableRow { Cells = new List<string> { "Date of Arrival", reductionFactor.DateOfArrival.ToString("yyyy-MM-dd") } },
    //            new ReportTableRow { Cells = new List<string> { "Duration (Hrs)", reductionFactor.Duration.ToString() } },
    //            new ReportTableRow { Cells = new List<string> { "ETD", reductionFactor.ETD.ToString("HH:mm") } },
    //            new ReportTableRow { Cells = new List<string> { "ETA", reductionFactor.ETA.ToString("HH:mm") } }
    //        }
    //        };
    //        reportData.Sections.Add(userInputsSection);

    //        // Weather Forecast Section
    //        var weatherSection = new ReportSection
    //        {
    //            Title = "Weather Forecast",
    //            Type = "table",
    //            TableData = new List<ReportTableRow>
    //        {
    //            new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
    //            new ReportTableRow { Cells = new List<string> { "Date", reductionFactor.WeatherForecastDate.ToString("yyyy-MM-dd") } },
    //            new ReportTableRow { Cells = new List<string> { "Time", reductionFactor.WeatherForecasetTime.ToString("HH:mm") } },
    //            new ReportTableRow { Cells = new List<string> { "Before ETD (Hrs)", reductionFactor.WeatherForecastBeforeETD.ToString() } },
    //            new ReportTableRow { Cells = new List<string> { "Source", reductionFactor.WeatherForecastSource ?? "www.weather.gov" } }
    //        }
    //        };
    //        reportData.Sections.Add(weatherSection);

    //        // Wave Height Section
    //        //var waveHeightSection = new ReportSection
    //        //{
    //        //    Title = "Forecast Maximum Significant Wave Height",
    //        //    Type = "table",
    //        //    TableData = new List<ReportTableRow>
    //        //{
    //        //    new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
    //        //    new ReportTableRow { Cells = new List<string> { "Hswell [m]", reductionFactor.WaveHeightHswell.ToString() } },
    //        //    new ReportTableRow { Cells = new List<string> { "Hwind [m]", reductionFactor.WaveHeightHwind.ToString() } },
    //        //    new ReportTableRow { Cells = new List<string> { "Hs, max [m]", reductionFactor.WaveHsmax.ToString() } }
    //        //}
    //        //};
    //        //reportData.Sections.Add(waveHeightSection);

    //        // Output Section
    //        var outputSection = new ReportSection
    //        {
    //            Title = "Output",
    //            Type = "text",
    //            Content = $"Duration OK: {reductionFactor.DurationOk}\n" +
    //                      $"Forecast Before ETD OK: {reductionFactor.WeatherForecastBeforeETDOK}\n" +
    //                      $"Reduction Factor: {reductionFactor.ShortVoyageReductionFactor?.ToString("0.00") ?? "N/A"}"
    //        };
    //        reportData.Sections.Add(outputSection);

    //        // Add notes
    //        reportData.Notes = new List<string>
    //    {
    //        "The vessel is to have CLP-V or CLP-VP(XR) notation, and the onboard Computer Lashing Program is to be approved to handle Short Voyage Reduction Factors.",
    //        "The minimum value of the Short Voyage Reduction Factor is 0.6 and needs to be included in Cargo Securing Manual (CSM).",
    //        "A short voyage is to have a duration of less than 72 hours from departure port to arrival port.",
    //        "The weather reports need to be received within 6 hours of departure.",
    //        "The forecasted wave height needs to cover the duration of the voyage plus 12 hours."
    //    };

    //        return reportData;
    //    }

    //    //For reduction factor
    //    private async Task DownloadReductionFactorReport()
    //    {
    //        try
    //        {
    //            // Create report data 
    //            var reportData = CreateReductionFactorReportData();

    //            // Generate file name
    //            string fileName = $"ReductionFactorReport_{DateTime.Now:yyyyMMdd}_{routeModel?.RouteName?.Replace(" ", "_") ?? "Route"}.pdf";

    //            // Call the PDF service
    //            await PdfService.DownloadPdfAsync(reportData, fileName);
    //        }
    //        catch (Exception ex)
    //        {
    //            // Handle exception
    //            Console.Error.WriteLine($"Error downloading reduction factor report: {ex.Message}");
    //        }
    //    }

    //    private ReportData CreateReductionFactorReportData()
    //    {
    //        var reportData = new ReportData
    //        {
    //            ReportName = "Route Reduction Factor Report",
    //            Title = HeadingText,
    //            AttentionText = $"Mr. Alan Bond, Mani Industries (WCN: 123456)"
    //        };

    //        // User Inputs Section
    //        var userInputsSection = new ReportSection
    //        {
    //            Title = "User Inputs",
    //            Type = "table",
    //            TableData = new List<ReportTableRow>
    //    {
    //        // Header row
    //        new ReportTableRow { Cells = new List<string> { "Parameter", "Value", "Code" } },
    //        // Data rows
    //        new ReportTableRow { Cells = new List<string> { "Route Name", routeModel?.RouteName ?? "Marseille - Shanghai", "" } },
    //        new ReportTableRow { Cells = new List<string> { "Port of Departure", routeModel?.MainDeparturePortSelection?.Port?.Name ?? "Marseille, France", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Unlocode ?? "FRMRS" } },
    //        new ReportTableRow { Cells = new List<string> { "Loading Port", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Name ?? "Singapore", routeModel?.DeparturePorts?.FirstOrDefault()?.Port?.Unlocode ?? "SGSIN" } },
    //        new ReportTableRow { Cells = new List<string> { "Port of Arrival", routeModel?.MainArrivalPortSelection?.Port?.Name ?? "Shanghai, China", routeModel?.ArrivalPorts?.FirstOrDefault()?.Port?.Unlocode ?? "CNSGH" } }
    //    }
    //        };
    //        reportData.Sections.Add(userInputsSection);

    //        // Output Section
    //        var outputSection = new ReportSection
    //        {
    //            Title = "Output",
    //            Type = "table"
    //        };

    //        if (isCLPVChecked)
    //        {
    //            // Single reduction factor for CLP-V
    //            outputSection.TableData = new List<ReportTableRow>
    //    {
    //        new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
    //        new ReportTableRow { Cells = new List<string> { "Reduction Factor", routeReductionFactor?.ToString("0.00") ?? "0.82" } }
    //    };
    //        }
    //        else if (isCLPVParrChecked)
    //        {
    //            // Seasonal reduction factors for CLP-VP(XR)
    //            outputSection.TableData = new List<ReportTableRow>
    //    {
    //        new ReportTableRow { Cells = new List<string> { "Season", "Reduction Factor" } }
    //    };

    //            foreach (var season in seasonalOptions)
    //            {
    //                outputSection.TableData.Add(new ReportTableRow
    //                {
    //                    Cells = new List<string> { season, GetReductionFactorForSeason(season) }
    //                });
    //            }
    //        }

    //        reportData.Sections.Add(outputSection);

    //        // Add notes
    //        reportData.Notes = new List<string>
    //{
    //    "The vessel is to have CLP-V or CLP-VP(XR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
    //    "ABS Container Securing Guide 6.2.4"
    //};

    //        return reportData;
    //    }
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



        /// added 
        // Complete Short Voyage Report Data Creation
        private async Task<ReportData> CreateCompleteShortVoyageReportData()
        {
            var reportData = new ReportData
            {
                ReportName = "Short Voyage Reduction Factor Report",
               // Title = HeadingTextShortVoyage,
                AttentionText = "Mr. Alan Bond, Mani Industries (WCN: 123456)",
                Description = $"Based on your inputs in the ABS Online Reduction Factor Tool, the calculated Reduction Factor for the voyage is {reductionFactor.ShortVoyageReductionFactor?.ToString("0.00") ?? "N/A"}. More details can be found below.",
                ContactInfo = "For any clarifications, contact Mr. Holland Wright at +65 6371 2xxx or (HWright@eagle.org)."
            };

            // Capture map image
            reportData.MapImage = await CaptureMapAsBytes();

            // User Inputs Section
            var userInputsSection = new ReportSection
            {
                Title = "User Inputs",
                Type = "table",
                TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value", "Additional Info" } },
            new ReportTableRow { Cells = new List<string> { "Vessel Name:", reductionFactor.VesselName ?? "N/A", "" } },
            new ReportTableRow { Cells = new List<string> { "Vessel IMO:", reductionFactor.IMONo ?? "N/A", "" } },
            new ReportTableRow { Cells = new List<string> { "Vessel Breadth:", reductionFactor.Breadth.ToString(), "" } },
            new ReportTableRow { Cells = new List<string> { "Port of Departure:", reductionFactor.PortOfDeparture ?? "N/A", "" } },
            new ReportTableRow { Cells = new List<string> { "Port of Arrival:", reductionFactor.PortOfArrival ?? "N/A", "" } },
            new ReportTableRow { Cells = new List<string> { "Time Zone:", reductionFactor.TimeZone ?? "UTC", "" } },
            new ReportTableRow { Cells = new List<string> { "Date of Departure:", reductionFactor.DateOfDeparture.ToString("yyyy-MM-dd"), "" } },
            new ReportTableRow { Cells = new List<string> { "Date of Arrival:", reductionFactor.DateOfArrival.ToString("yyyy-MM-dd"), "" } },
            new ReportTableRow { Cells = new List<string> { "Duration (Hrs):", reductionFactor.Duration.ToString(), "" } },
            new ReportTableRow { Cells = new List<string> { "Estimated Time of Departure:", reductionFactor.ETD.ToString("HH:mm"), "" } },
            new ReportTableRow { Cells = new List<string> { "Estimated Time of Arrival:", reductionFactor.ETA.ToString("HH:mm"), "" } }
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
            new ReportTableRow { Cells = new List<string> { "Date:", reductionFactor.WeatherForecastDate.ToString("yyyy-MM-dd") } },
            new ReportTableRow { Cells = new List<string> { "Time:", reductionFactor.WeatherForecasetTime.ToString("HH:mm") } },
            new ReportTableRow { Cells = new List<string> { "Before ETD (Hrs):", reductionFactor.WeatherForecastBeforeETD.ToString() } },
            new ReportTableRow { Cells = new List<string> { "Source:", reductionFactor.WeatherForecastSource ?? "www.weather.gov" } }
        }
            };
            reportData.Sections.Add(weatherSection);

            // Wave Height Section
            var waveHeightSection = new ReportSection
            {
                Title = "Forecast Maximum Significant Wave Height",
                Type = "table",
                TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value" } },
            new ReportTableRow { Cells = new List<string> { "Hswell [m]:", reductionFactor.WaveHeightHswell.ToString() } },
            new ReportTableRow { Cells = new List<string> { "Hwind [m]:", reductionFactor.WaveHeightHwind.ToString() } },
            new ReportTableRow { Cells = new List<string> { "Hs, max [m]:", reductionFactor.WaveHsmax.ToString() } }
        }
            };
            reportData.Sections.Add(waveHeightSection);

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

            // Map Section
            var mapSection = new ReportSection
            {
                Title = "Voyage Map",
                Type = "image",
                ImageType = "map",
                ImageData = reportData.MapImage
            };
            reportData.Sections.Add(mapSection);

            // Reduction Factor Result Section
            var resultSection = new ReportSection
            {
                Title = "Limited Short Voyage Reduction Factor",
                Type = "text",
                Content = $"Reduction Factor: {reductionFactor.ShortVoyageReductionFactor?.ToString("0.00") ?? "N/A"}"
            };
            reportData.Sections.Add(resultSection);

            // Add chart if available
            var chartBytes = await CaptureChartAsBytes("reductionChart2");
            if (chartBytes != null)
            {
                var chartSection = new ReportSection
                {
                    Title = "Reduction Factor Chart",
                    Type = "image",
                    ImageType = "chart",
                    ImageData = chartBytes
                };
                reportData.Sections.Add(chartSection);
            }

            // Notes
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

        // Complete Reduction Factor Report Data Creation
        private async Task<ReportData> CreateCompleteReductionFactorReportData()
        {
            var reportData = new ReportData
            {
                ReportName = "Route Reduction Factor Report",
                Title = HeadingText,
                AttentionText = "Mr. Alan Bond, Mani Industries (WCN: 123456)",
                Description = $"Based on your inputs in the ABS Online Reduction Factor Tool, the calculated Reduction Factor for the route from {routeModel?.DepartureLocation} to {routeModel?.ArrivalLocation} is {routeReductionFactor?.ToString("0.00") ?? "N/A"}. More details can be found below.",
                ContactInfo = "For any clarifications, contact Mr. Holland Wright at +65 6371 2xxx or (HWright@eagle.org)."
            };

            // Capture map image
            reportData.MapImage = await CaptureMapAsBytes();

            // User Inputs Section
            var userInputsSection = new ReportSection
            {
                Title = "User Inputs",
                Type = "table",
                TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value", "Code" } },
            new ReportTableRow { Cells = new List<string> { "Route Name:", routeModel?.RouteName ?? "N/A", "" } },
            new ReportTableRow { Cells = new List<string> { "Port of Departure:", routeModel?.MainDeparturePortSelection?.Port?.Name ?? "N/A", routeModel?.MainDeparturePortSelection?.Port?.Unlocode ?? "" } }
        }
            };

            // Add departure ports if any
            if (routeModel?.DeparturePorts != null && routeModel.DeparturePorts.Count > 0)
            {
                for (int i = 0; i < routeModel.DeparturePorts.Count; i++)
                {
                    var port = routeModel.DeparturePorts[i]?.Port;
                    userInputsSection.TableData.Add(new ReportTableRow
                    {
                        Cells = new List<string> { $"Loading Port {i + 1}:", port?.Name ?? "Unknown", port?.Unlocode ?? "N/A" }
                    });
                }
            }

            userInputsSection.TableData.Add(new ReportTableRow
            {
                Cells = new List<string> { "Port of Arrival:", routeModel?.MainArrivalPortSelection?.Port?.Name ?? "N/A", routeModel?.MainArrivalPortSelection?.Port?.Unlocode ?? "" }
            });

            reportData.Sections.Add(userInputsSection);

            // Main Results Section
            if (isCLPVChecked)
            {
                // Simple reduction factor display for CLP-V
                var simpleResultSection = new ReportSection
                {
                    Title = "Results",
                    Type = "text",
                    //Content = $"Reduction Factor: {decimal.Round(decimal.Max((decimal)routeModel?.ReductionFactor, 0.80m), 2)}"
                };
                reportData.Sections.Add(simpleResultSection);
            }
            else
            {
                // Complex table for CLP-VP(XR) - matching the UI exactly
                var complexTableSection = new ReportSection
                {
                    Title = "Route Analysis",
                    Type = "complex-table",
                    ComplexTable = CreateRouteAnalysisTable()
                };
                reportData.Sections.Add(complexTableSection);
            }

            // Map Section
            var mapSection = new ReportSection
            {
                Title = "Route Map",
                Type = "image",
                ImageType = "map",
                ImageData = reportData.MapImage
            };
            reportData.Sections.Add(mapSection);

            // Notes
            reportData.Notes = new List<string>
    {
        "The vessel is to have CLP-V or CLP-VP(XR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
        "ABS Container Securing Guide 6.2.4"
    };

            return reportData;
        }

        // Create the complex route analysis table matching the UI

        //new
        private ComplexTableData CreateRouteAnalysisTable()
        {
            var complexTable = new ComplexTableData();

            // Headers - matching the complex structure in the UI
            var headerRow1 = new ComplexTableRow
            {
                IsHeaderRow = true,
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "", RowSpan = 2 },
            new ComplexTableCell { Text = "", ColumnSpan = 3 },
            new ComplexTableCell { Text = "Seasonal Reduction Factor", ColumnSpan = 4, IsBold = true }
        }
            };

            var headerRow2 = new ComplexTableRow
            {
                IsHeaderRow = true,
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "Routes", IsBold = true },
            new ComplexTableCell { Text = "Distance", IsBold = true },
            new ComplexTableCell { Text = "Annual Reduction Factor", IsBold = true },
            new ComplexTableCell { Text = "Spring", IsBold = true },
            new ComplexTableCell { Text = "Summer", IsBold = true },
            new ComplexTableCell { Text = "Fall", IsBold = true },
            new ComplexTableCell { Text = "Winter", IsBold = true }
        }
            };

            complexTable.Rows.Add(headerRow1);
            complexTable.Rows.Add(headerRow2);

            // Entire Route Row
            var entireRouteRow = new ComplexTableRow
            {
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "Entire Route", IsBold = true },
            new ComplexTableCell { Text = $"{routeModel?.MainDeparturePortSelection?.Port?.Name} - {routeModel?.MainArrivalPortSelection?.Port?.Name}" },
            new ComplexTableCell { Text = $"{Math.Round(routeModel?.TotalDistance ?? 0)} nm" },
           // new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Annual"), IsBold = true },
            //new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Spring") },
            //new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Summer") },
            //new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Fall") },
            //new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Winter") }
        }
            };
            complexTable.Rows.Add(entireRouteRow);

            // Route Splitting Rows
            //if (routeLegs != null && routeLegs.Any())
            //{
            //    bool isFirst = true;
            //    foreach (var leg in routeLegs)
            //    {
            //        var legRow = new ComplexTableRow
            //        {
            //            Cells = new List<ComplexTableCell>()
            //        };

            //        if (isFirst)
            //        {
            //            legRow.Cells.Add(new ComplexTableCell { Text = "Route Splitting", RowSpan = routeLegs.Count, IsBold = true });
            //            isFirst = false;
            //        }

            //        legRow.Cells.AddRange(new List<ComplexTableCell>
            //{
            //    new ComplexTableCell { Text = $"{leg.DeparturePort} - {leg.ArrivalPort}" },
            //    new ComplexTableCell { Text = $"{Math.Round(leg.Distance)} nm" },
            //    new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Annual"), IsBold = true },
            //    new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Spring") },
            //    new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Summer") },
            //    new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Fall") },
            //    new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Winter") }
            //});

            //        complexTable.Rows.Add(legRow);
            //    }
            //}

            return complexTable;
        }

        // Improved C# methods with better error handling and timeout management

        private async Task<byte[]> CaptureMapAsBytes()
        {
            try
            {
                // Add timeout to prevent TaskCanceledException
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Get the existing captured image (from captureMapWithRoutes)
                var base64String = await JS.InvokeAsync<string>("getLatestMapImage", cts.Token);

                if (!string.IsNullOrEmpty(base64String))
                {
                    // Remove data URL prefix if present
                    if (base64String.StartsWith("data:image"))
                    {
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }

                    // Validate base64 string before conversion
                    if (IsValidBase64String(base64String))
                    {
                        return Convert.FromBase64String(base64String);
                    }
                    else
                    {
                        Console.WriteLine("Invalid base64 string received from JavaScript");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("No map image available. Please capture screenshot first.");
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Map image capture timed out. Please try again.");
                return null;
            }
            catch (JSException jsEx)
            {
                Console.WriteLine($"JavaScript error getting map image: {jsEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting map image: {ex.Message}");
                return null;
            }
        }

        private bool IsValidBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return false;

            try
            {
                // Check if string length is multiple of 4
                if (base64.Length % 4 != 0)
                    return false;

                // Try to convert to test validity
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        } 



        private async Task<byte[]> CaptureChartAsBytes(string chartId)
        {
            try
            {
                var base64String = await JS.InvokeAsync<string>("captureChartAsBase64", chartId);
                if (!string.IsNullOrEmpty(base64String))
                {
                    if (base64String.StartsWith("data:image"))
                    {
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }
                    return Convert.FromBase64String(base64String);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing chart: {ex.Message}");
            }
            return null;
        }

        // Updated download methods
        private async Task DownloadCompleteReport()
        {
            try
            {
                var reportData = await CreateCompleteShortVoyageReportData();
                string fileName = $"ShortVoyageReport_{DateTime.Now:yyyyMMdd}_{reductionFactor.VesselName}.pdf";
                await PdfService.DownloadPdfAsync(reportData, fileName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error downloading report: {ex.Message}");
            }
        }

        private async Task DownloadCompleteReductionFactorReport()
        {
            try
            {
                var reportData = await CreateCompleteReductionFactorReportData();
                string fileName = $"ReductionFactorReport_{DateTime.Now:yyyyMMdd}_{routeModel?.RouteName?.Replace(" ", "_") ?? "Route"}.pdf";
                await PdfService.DownloadPdfAsync(reportData, fileName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error downloading reduction factor report: {ex.Message}");
            }
        }


        //added
        private async Task<byte[]> CaptureMapWithRoutesAsBytes(string mapElementId = "reportMapContainer", string displayElementId = null)
        {
            try
            {
                // Use the enhanced map capture function
                var base64String = await JS.InvokeAsync<string>("captureMapWithRoutes", mapElementId, displayElementId);

                if (!string.IsNullOrEmpty(base64String))
                {
                    // Remove data URL prefix if present
                    if (base64String.StartsWith("data:image"))
                    {
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }
                    return Convert.FromBase64String(base64String);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing map with routes: {ex.Message}");
                Console.WriteLine($"Full exception: {ex}");
            }
            return null;
        }


        private async Task<bool> EnsureChartsReady()
        {
            try
            {
                // Check if Chart.js is loaded
                var chartJsLoaded = await JS.InvokeAsync<bool>("eval", "typeof Chart !== 'undefined'");
                if (!chartJsLoaded)
                {
                    Console.WriteLine("Chart.js is not loaded");
                    return false;
                }

                // You can add more checks here if needed
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking chart readiness: {ex.Message}");
                return false;
            }
        }
        private async Task<byte[]> CaptureChartDirectAsBytes(string canvasId)
        {
            try
            {
                // Use the Chart.js canvas directly
                var base64String = await JS.InvokeAsync<string>("getChartAsBase64", canvasId);

                if (!string.IsNullOrEmpty(base64String))
                {
                    if (base64String.StartsWith("data:image"))
                    {
                        base64String = base64String.Substring(base64String.IndexOf(",") + 1);
                    }
                    return Convert.FromBase64String(base64String);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing chart directly {canvasId}: {ex.Message}");
                Console.WriteLine($"Full exception: {ex}");
            }
            return null;
        }

        //private async Task<bool> EnsureChartsReady()
        //{
        //    try
        //    {
        //        // Check if Chart.js is loaded
        //        var chartJsLoaded = await JS.InvokeAsync<bool>("eval", "typeof Chart !== 'undefined'");
        //        if (!chartJsLoaded)
        //        {
        //            Console.WriteLine("Chart.js is not loaded");
        //            return false;
        //        }

        //        // You can add more checks here if needed
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error checking chart readiness: {ex.Message}");
        //        return false;
        //    }
        //}

    }
}
