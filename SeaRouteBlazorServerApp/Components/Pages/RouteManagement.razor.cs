using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using NextGenEngApps.DigitalRules.CRoute.Models;
using SeaRouteModel.Models;


namespace NextGenEngApps.DigitalRules.CRoute.Components.Pages
{
    public partial class Home
    {
        //parent component
        private ElementReference reportMapContainer;
        private int selectedTab = 1;
        private int selectedForm = 0;
        private ReductionFactorCalculation? reductionFactorCalRef;
        private ReductionFactor reductionFactor = new ReductionFactor();
        private DotNetObjectReference<Home>? objRef;
        private void SelectTab(int tab) => InitializeScreen(tab);
        private void CloseOverlay() { /* Logic to hide overlay */ }
        private List<PortModel> _ports;
        private decimal? routeReductionFactor = 0.82M;
        private RouteModel routeModel = new RouteModel();
        private List<VesselInfo> vesselInfos = new List<VesselInfo>();
        private List<RouteLegModel> routeLegs = new List<RouteLegModel>();
        private List<string> seasonalOptions = new() { "Annual", "Spring", "Summer", "Fall", "Winter" };
        private Dictionary<string, List<string>> seasonMonths = new()
        {
         { "Spring", new List<string> { "Mar", "May" } },
         { "Summer", new List<string> { "Jun",  "Aug" } },
         { "Fall", new List<string> { "Sep",  "Nov" } },
         { "Winter", new List<string> { "Dec", "Feb" } }
        };
        private bool isCLPVChecked = false;
        private bool isCLPVParrChecked = false;
        private string HeadingText { get; set; } = "Report 1 Reduction Factor Calculation";
        private string HeadingTextShortVoyage { get; set; } = "Report 1 Reduction Factor Calculation";
        private string OriginalText { get; set; } = "Report 1 Reduction Factor Calculation";
        private bool IsEditing { get; set; } = false;
        private InputFile fileInput;
        private byte[] capturedImageBytes;
        public string ReportMessage { get; set; }
        public bool IsMailSent { get; set; } = false;
        public bool IsSubmissionComplete { get; set; } = false;
        public string EditRouteId { get; set; }
        private VoyageLegReductionFactorResponse _reductionFactorResponse;
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
        private void ShowForm(int option)
        {
            EditRouteId = string.Empty;
            selectedForm = option;
            StateHasChanged();
        }
        private bool showReport = false;
        private bool showReportForReductionFactor = false;
        private bool isVesselSaved = false;
        private bool ShowSubmissionUI = false;

        private int zIndexCounter = 1000;
        protected async override Task OnInitializedAsync()
        {

            //if (vesselInfos.Count == 0)
            //{
            //    AddNewVessel();
            //}
            var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

            if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var param))
            {

                if (int.TryParse(param, out var parsedId))

                {


                    navigationManager.NavigateTo("/", forceLoad: false, replace: true);
                    selectedTab = parsedId;
                }

            }
            await Task.CompletedTask;
        }

        private async Task HandleReductionFactorData(VoyageLegReductionFactorResponse reductionFactorResponse)
        {
            _reductionFactorResponse = reductionFactorResponse;
            StateHasChanged();

        }
        private async Task OnCheckboxChanged(ChangeEventArgs e, string checkboxType)
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
            if (showReportForReductionFactor)
            {
                await Task.Delay(200);
                await CaptureMap();
            }


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

        private double GetSeasonalReductionFactor(string season, bool isRoute = true, int legOrder = 0)
        {
            if (_reductionFactorResponse == null) return 0.0;

            ReductionFactors factors = null;

            if (isRoute)
            {
                factors = _reductionFactorResponse.Route?.ReductionFactors;
            }
            else
            {
                var leg = _reductionFactorResponse.VoyageLegs?.FirstOrDefault(vl => vl.VoyageLegOrder == legOrder);
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
        private async Task AddNewVessel()
        {
            vesselInfos.Add(new VesselInfo());

            await Task.CompletedTask;
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

        [JSInvokable]
        public async Task EnableCalculate()
        {
            await reductionFactorCalRef!.EnableCalculateButton();
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
            if (routeModel.RouteId != null)
            {
                string routeId = routeModel.RouteId;
                Guid.TryParse(routeId, out Guid routeGuid);
                string userId = Services.API.Endpoints.USERID;
                foreach (var item in vesselInfos)
                {
                    if (!string.IsNullOrEmpty(item.VesselName) && !string.IsNullOrEmpty(item.IMONumber))
                    {
                        int.TryParse(item.IMONumber, out int vesselIMO);
                        isVesselSaved = await routeAPIService.SaveVesselAsync(
                            new Services.API.Request.AddVessel(routeId, string.Empty, item.VesselName, item.IMONumber,
                            item.Breadth, item.Flag, userId));
                    }
                }
                StateHasChanged();
            }
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
            vesselInfos = [];
            _reductionFactorResponse = null;
            showReport = false;
            showReportForReductionFactor = false;
            ShowABSReport = false;
            await Task.CompletedTask;
        }
        private async Task AddEditVesselInfo(string routeId)
        {
            isVesselSaved = false;
            if (!string.IsNullOrEmpty(routeId))
            {
                routeModel.RouteId = routeId;
                vesselInfos = [];
                var vessels = await routeAPIService.GetVesselListByRecordAsync(routeModel.RouteId);
                if (vessels.Count > 0)
                {
                    vesselInfos.Add(new VesselInfo()
                    {
                        IMONumber = vessels[0].IMONumber.ToString(),
                        VesselName = vessels[0].VesselName,
                        Flag = vessels[0].Flag
                    });
                }
                else
                    await AddNewVessel();
            }
            else
            {
                await AddNewVessel();
            }
            StateHasChanged();
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
            "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Short Voyage Reduction Factors.",
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
                // Seasonal reduction factors for CLP-V(PARR)
                outputSection.TableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Season", "Reduction Factor" } }
        };

                foreach (var season in seasonalOptions)
                {
                    outputSection.TableData.Add(new ReportTableRow
                    {
                        Cells = new List<string> { season, GetCorrectedReductionFactor((decimal)routeModel.ReductionFactor, season) }
                    });
                }
            }

            reportData.Sections.Add(outputSection);

            // Add notes
            reportData.Notes = new List<string>
    {
        "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
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
        private List<ShowABSReportForm> vesselImos = new List<ShowABSReportForm>();
        public class ShowABSReportForm
        {
            public string ImoNumber { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public string Flag { get; set; } = string.Empty;
            public string ReportDate { get; set; } = string.Empty;
        }
        private void CloseABSReportForm()
        {
            ShowABSReport = false;
            ShowSubmissionUI = false;
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
        private void AddNewImo()
        {
            vesselImos.Add(new ShowABSReportForm { ImoNumber = "", VesselName = "" });
            StateHasChanged();
        }
        private async Task SubmitABSReport()
        {
            if (vesselImos.Count == 0)
            {
                ReportMessage = "The report has no subject vessels. Please add at least one vessel to the report before submission to ABS";
            }
            else
            {
                ShowSubmissionUI = true;
                IsSubmissionComplete = false;
                IsMailSent = await SubmitReport();
                IsSubmissionComplete = true;
                ReportMessage = string.Empty;
            }
        }

        private async Task<bool> SubmitReport()
        {
            try
            {
                var reportData = await CreateCompleteReductionFactorReportData();
                string fileName = $"ReductionFactorReport_{DateTime.Now:yyyyMMdd}_{routeModel?.RouteName?.Replace(" ", "_") ?? "Route"}.pdf";
                byte[] pdfBytes = await PdfService.GenerateReportPdfAsync(reportData);
                string base64 = Convert.ToBase64String(pdfBytes);
                string userEmail = "";
                string userId = "1";
                var report = new Services.API.Request.SubmitReport(routeModel!.RouteId, userEmail, "",
                    fileName, userId, base64);
                return await routeAPIService.SubmitABSReportAsync(report);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ShowAbsReport(string routeId)
        {
            ShowABSReport = true;
            vesselImos.Clear();
            ReportMessage = string.Empty;
            if (!string.IsNullOrEmpty(routeId))
            {
                var vessels = await routeAPIService.GetVesselListByRecordAsync(routeId);
                if (vessels != null)
                {
                    foreach (var vessel in vessels)
                    {
                        vesselImos.Add(new ShowABSReportForm()
                        {
                            ImoNumber = !string.IsNullOrEmpty(vessel.IMONumber) ? vessel.IMONumber!.ToString() : string.Empty,
                            VesselName = vessel.VesselName,
                            Flag = vessel.Flag ?? string.Empty,
                            //ReportDate = vessel. DateTime.Now.ToString("yyyy-MM-dd")
                        });
                    }
                }
            }
        }
        private async Task HandleReductionReportData(RouteModel _routeModel)
        {
            routeModel = _routeModel;
            StateHasChanged();
        }
        private async Task ShowPopUp(string divId)
        {
            zIndexCounter = zIndexCounter + 5;
            await JS.InvokeVoidAsync("setZIndex", divId, zIndexCounter);
        }
        private async Task HandleReductionLegsReportData(List<RouteLegModel> _routeLegs)
        {
            routeLegs = _routeLegs;
            StateHasChanged();
        }

        // test

        // Complete Short Voyage Report Data Creation
        private async Task<ReportData> CreateCompleteShortVoyageReportData()
        {
            var reportData = new ReportData
            {
                ReportName = "Short Voyage Reduction Factor Report",
                Title = HeadingTextShortVoyage,
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
        "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Short Voyage Reduction Factors.",
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

            userInputsSection.TableData.Add(new ReportTableRow
            {
                Cells = new List<string> { "Vessel Name:", !string.IsNullOrEmpty(routeModel!.Vessel!.VesselName) ? routeModel!.Vessel!.VesselName : "", "" }
            });
            userInputsSection.TableData.Add(new ReportTableRow
            {
                Cells = new List<string> { "Vessel IMO:", !string.IsNullOrEmpty(routeModel!.Vessel!.IMONumber) ? routeModel!.Vessel!.IMONumber.ToString() : "", "" }
            });
            userInputsSection.TableData.Add(new ReportTableRow
            {
                Cells = new List<string> { "Vessel Flag:", !string.IsNullOrEmpty(routeModel!.Vessel!.Flag) ? routeModel!.Vessel!.Flag : "", "" }
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
                    Content = $"Reduction Factor: {decimal.Round(decimal.Max((decimal)routeModel?.ReductionFactor, 0.80m), 2)}"
                };
                reportData.Sections.Add(simpleResultSection);
            }
            else
            {
                // Complex table for CLP-V(PARR) - matching the UI exactly
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
                "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
                "ABS Container Securing Guide 6.2.4"
            };

            return reportData;
        }

        // Create the complex route analysis table matching the UI
        private ComplexTableData CreateRouteAnalysisTable()
        {
            var complexTable = new ComplexTableData();

            // Headers - fixed structure to properly handle column spans
            var headerRow1 = new ComplexTableRow
            {
                IsHeaderRow = true,
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "", RowSpan = 2 }, // Row label column
            new ComplexTableCell { Text = "", RowSpan = 2 }, // Routes column  
            new ComplexTableCell { Text = "", RowSpan = 2 }, // Distance column
            new ComplexTableCell { Text = "", RowSpan = 2 }, // Annual Reduction Factor column
            new ComplexTableCell { Text = "Seasonal Reduction Factor", ColumnSpan = 4, IsBold = true }
        }
            };

            //    var headerRow2 = new ComplexTableRow
            //    {
            //        IsHeaderRow = true,
            //        Cells = new List<ComplexTableCell>
            //{
            //    // Skip the first 4 columns due to RowSpan in headerRow1
            //    new ComplexTableCell { Text = "Spring", IsBold = true },
            //    new ComplexTableCell { Text = "Summer", IsBold = true },
            //    new ComplexTableCell { Text = "Fall", IsBold = true },
            //    new ComplexTableCell { Text = "Winter", IsBold = true }
            //}
            //    };

            // Add a proper third header row with all column labels
            var headerRow3 = new ComplexTableRow
            {
                IsHeaderRow = true,
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "", IsBold = true }, // Row label
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
            //complexTable.Rows.Add(headerRow2);
            complexTable.Rows.Add(headerRow3);

            // Entire Route Row
            var entireRouteRow = new ComplexTableRow
            {
                Cells = new List<ComplexTableCell>
        {
            new ComplexTableCell { Text = "Entire Route", IsBold = true },
            new ComplexTableCell { Text = $"{routeModel?.MainDeparturePortSelection?.Port?.Name} - {routeModel?.MainArrivalPortSelection?.Port?.Name}" },
            new ComplexTableCell { Text = $"{Math.Round(routeModel?.TotalDistance ?? 0)} nm" },
            new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Annual"), IsBold = true },
            new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Spring") },
            new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Summer") },
            new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Fall") },
            new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)(routeModel?.ReductionFactor ?? 0), "Winter") }
        }
            };
            complexTable.Rows.Add(entireRouteRow);

            // Route Splitting Rows - Fixed to handle proper column alignment
            if (routeLegs != null && routeLegs.Any())
            {
                bool isFirst = true;
                foreach (var leg in routeLegs)
                {
                    var legRow = new ComplexTableRow
                    {
                        Cells = new List<ComplexTableCell>()
                    };

                    if (isFirst)
                    {
                        legRow.Cells.Add(new ComplexTableCell { Text = "Route Splitting", RowSpan = routeLegs.Count, IsBold = true });
                        isFirst = false;
                    }
                    else
                    {
                        // Add empty cell for non-first rows to maintain column alignment
                        legRow.Cells.Add(new ComplexTableCell { Text = "" });
                    }

                    legRow.Cells.AddRange(new List<ComplexTableCell>
            {
                new ComplexTableCell { Text = $"{leg.DeparturePort} - {leg.ArrivalPort}" },
                new ComplexTableCell { Text = $"{Math.Round(leg.Distance)} nm" },
                new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Annual"), IsBold = true },
                new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Spring") },
                new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Summer") },
                new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Fall") },
                new ComplexTableCell { Text = GetCorrectedReductionFactor((decimal)leg.ReductionFactor, "Winter") }
            });

                    complexTable.Rows.Add(legRow);
                }
            }

            return complexTable;
        }

        // Helper methods for capturing images
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
                // Add timeout to prevent TaskCanceledException
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var base64String = await JS.InvokeAsync<string>("getChartAsBase64", cts.Token, chartId);
                if (!string.IsNullOrEmpty(base64String))
                {
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
            return null;
        }

        // Unified report generation for both Show and Download
        private UnifiedReportData BuildUnifiedReportData()
        {
            var data = new UnifiedReportData
            {
                Title = HeadingText,
                Attention = "Mr. Alan Bond, Mani Industries (WCN: 123456)",
                Body = $"Based on your inputs in the ABS Online Reduction Factor Tool, the calculated Reduction Factor for the route from {routeModel?.MainDeparturePortSelection?.Port?.Name} to {routeModel?.MainArrivalPortSelection?.Port?.Name} is {routeModel?.ReductionFactor?.ToString("0.00") ?? "N/A"}. More details can be found below.",
                Notes = new List<string>
                {
                    "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.",
                    "ABS Container Securing Guide 6.2.4"
                }
            };
            // User Inputs Section
            var userInputs = new UnifiedReportSection
            {
                Heading = "User Inputs",
                Type = "table",
                Table = new List<KeyValuePair<string, string>>
                {
                    new("Route Name", routeModel?.RouteName ?? "N/A"),
                    new("Report Date", routeModel?.ReportDate?.ToString("yyyy-MM-dd") ?? "N/A"),
                    new("Port of Departure", routeModel?.MainDeparturePortSelection?.Port?.Name + " (" + routeModel?.MainDeparturePortSelection?.Port?.Unlocode + ")"),
                    new("Port of Arrival", routeModel?.MainArrivalPortSelection?.Port?.Name + " (" + routeModel?.MainArrivalPortSelection?.Port?.Unlocode + ")")
                }
            };
            data.Sections.Add(userInputs);
            // Vessel Info Section
            if (routeModel?.Vessel != null)
            {
                var vesselInfo = new UnifiedReportSection
                {
                    Heading = "Vessel Info",
                    Type = "table",
                    Table = new List<KeyValuePair<string, string>>
                    {
                        new("Vessel Name", routeModel.Vessel.VesselName),
                        new("IMO", routeModel.Vessel.IMONumber),
                        new("Flag", routeModel.Vessel.Flag)
                    }
                };
                data.Sections.Add(vesselInfo);
            }
            // Route Info Section (if routeLegs available)
            if (routeLegs != null && routeLegs.Count > 0)
            {
                var routeInfo = new UnifiedReportSection
                {
                    Heading = "Route Info",
                    Type = "table",
                    Table = routeLegs.Select(leg => new KeyValuePair<string, string>(
                        $"{leg.DeparturePort} - {leg.ArrivalPort}",
                        $"{Math.Round(leg.Distance)} nm"
                    )).ToList()
                };
                data.Sections.Add(routeInfo);
            }
            // Output Section (dynamic based on notation)
            var output = new UnifiedReportSection { Heading = "Output", Type = "table" };
            if (isCLPVChecked)
            {
                output.Table.Add(new KeyValuePair<string, string>("Reduction Factor", routeModel?.ReductionFactor?.ToString("0.00") ?? "N/A"));
            }
            else if (isCLPVParrChecked)
            {
                foreach (var season in seasonalOptions)
                {
                    output.Table.Add(new KeyValuePair<string, string>(season, GetCorrectedReductionFactor((decimal)routeModel.ReductionFactor, season)));
                }
            }
            data.Sections.Add(output);
            // Add map image if available
            if (reportModel?.MapImage != null)
            {
                data.Sections.Add(new UnifiedReportSection
                {
                    Heading = "Route Map",
                    Type = "image",
                    ImageData = reportModel.MapImage
                });
            }
            return data;
        }
        // Use this in Show Report and Download Report
        private async Task DownloadUnifiedReport()
        {
            var reportData = BuildUnifiedReportData();
            string fileName = $"ReductionFactorReport_{DateTime.Now:yyyyMMdd}_{routeModel?.RouteName?.Replace(" ", "_") ?? "Route"}.pdf";
            await PdfService.DownloadPdfAsync(reportData, fileName);
        }

        private async void InitializeScreen(int tab)
        {
            ShowABSReport = false;
            selectedTab = tab;
            await GoBack();
            await JS.InvokeVoidAsync("resetMap");
        }

        private async Task HandleEditRoute(string routeId)
        {
            try
            {
                if (!string.IsNullOrEmpty(routeId))
                {
                    EditRouteId = routeId;
                    selectedTab = 1;
                    selectedForm = 1;
                    StateHasChanged();
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
