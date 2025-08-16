// 1. Response Models for the Complete Report
// ResponseObjects/CompleteReportResponse.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class CompleteReportResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public ReportData Report { get; set; } = new();
    }

    public class ReportData
    {
        public string Title { get; set; } = string.Empty;
        public string DownloadTimestamp { get; set; } = string.Empty;
        public ReportSections Sections { get; set; } = new();
    }

    public class ReportSections
    {
        public AttentionSection Attention { get; set; } = new();
        public UserInputsSection UserInputs { get; set; } = new();
        public List<RouteAnalysisSegment> RouteAnalysis { get; set; } = new();
        public List<ReportNote> Notes { get; set; } = new();
    }

    public class AttentionSection
    {
        public string Salutation { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string AbsContact { get; set; } = string.Empty;
    }

    public class UserInputsSection
    {
        public ReportInfoSection ReportInfo { get; set; } = new();
        public VesselSection Vessel { get; set; } = new();
        public List<string> Ports { get; set; } = new();
    }

    public class ReportInfoSection
    {
        public string RouteName { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
    }

    public class VesselSection
    {
        public string Imo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
    }

    public class RouteAnalysisSegment
    {
        public SegmentInfo Segment { get; set; } = new();
    }

    public class SegmentInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public double Distance { get; set; }
        public ReductionFactorsInfo ReductionFactors { get; set; } = new();
    }

    public class ReductionFactorsInfo
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }

    public class ReportNote
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VesselCriteria { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GuideNote { get; set; }
    }
}

// 2. Updated IRouteVersionService Interface
namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IRouteVersionService
    {
        // Existing methods
        Task<RouteVersionLegs> GetVoyageLegsAsync(string routeVersionId);
        Task<RouteVersionResponse?> GetRouteVersionDetailsAsync(string routeVersionId);
        Task<RouteVersionReductionFactors> GetVoyageLegReductionFactors(string routeVersionId);

        // New method for complete report
        Task<CompleteReportResponse?> GetCompleteReportDataAsync(string routeVersionId);
    }
}

// 3. Updated RouteVersionService with Complete Report Method
namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class RouteVersionService : IRouteVersionService
    {
        private readonly IRouteVersionRepository _routeVersionRepository;
        private readonly IRecordService _recordService; // Inject to use existing record methods
        private readonly ILogger<RouteVersionService> _logger;

        public RouteVersionService(
            IRouteVersionRepository routeVersionRepository,
            IRecordService recordService,
            ILogger<RouteVersionService> logger)
        {
            _routeVersionRepository = routeVersionRepository ?? throw new ArgumentNullException(nameof(routeVersionRepository));
            _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Existing methods remain unchanged...
        public async Task<RouteVersionLegs> GetVoyageLegsAsync(string routeVersionId)
        {
            try
            {
                RouteVersionLegs routeVersionLegs = new()
                {
                    RouteVersionId = routeVersionId
                };

                _ = Guid.TryParse(routeVersionId, out Guid routeVersionGuId);
                var legs = await _routeVersionRepository.GetVoyageLegsAsync(routeVersionGuId);

                List<VoyageLeg> routeLegs = [];
                foreach (var le in legs)
                {
                    routeLegs.Add(new VoyageLeg()
                    {
                        VoyageLegOrder = le.LegOrder,
                        DeparturePortCode = le.DeparturePortCode,
                        DeparturePortName = le.DeparturePort,
                        ArrivalPortCode = le.ArrivalPortCode,
                        ArrivalPortName = le.ArrivalPort,
                        Distance = le.RouteDistance,
                    });
                }

                routeVersionLegs.VoyageLegs = [.. routeLegs];
                return routeVersionLegs;
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while fetching voyage legs for route version {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        public async Task<RouteVersionResponse?> GetRouteVersionDetailsAsync(string routeVersionId)
        {
            try
            {
                if (!Guid.TryParse(routeVersionId, out Guid routeVersionGuid))
                    return null;

                var routeVersionDto = await _routeVersionRepository.GetRouteVersionDetailsAsync(routeVersionGuid);

                if (routeVersionDto == null)
                    return null;

                return new RouteVersionResponse
                {
                    RouteVersionId = routeVersionGuid.ToString(),
                    RecordId = routeVersionDto.RecordId.ToString() ?? string.Empty,
                    RecordDate = routeVersionDto.RecordDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<RouteVersionReductionFactors> GetVoyageLegReductionFactors(string routeVersionId)
        {
            try
            {
                RouteVersionReductionFactors rvRfs = new()
                {
                    RouteVersionId = routeVersionId
                };

                _ = Guid.TryParse(routeVersionId, out Guid routeVersionGuId);
                var legRfs = await _routeVersionRepository.GetVoyageLegsReductionFactorsAsync(routeVersionGuId);

                List<VoyageLegReductionFactor> vlRfs = [];
                foreach (var le in legRfs)
                {
                    vlRfs.Add(new VoyageLegReductionFactor()
                    {
                        VoyageLegOrder = le.Order,
                        ReductionFactors = new ReductionFactors()
                        {
                            Annual = le.Annual,
                            Winter = le.Winter,
                            Spring = le.Spring,
                            Summer = le.Summer,
                            Fall = le.Fall
                        }
                    });
                }

                rvRfs.VoyageLegReductionFactors = vlRfs;
                return rvRfs;
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while fetching voyage legs for route version {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        // New Complete Report Method - Refactored based on team feedback
        public async Task<CompleteReportResponse?> GetCompleteReportDataAsync(string routeVersionId)
        {
            try
            {
                // Step 1: Get Route Version Details to obtain RecordId
                var routeVersionDetails = await GetRouteVersionDetailsAsync(routeVersionId);
                if (routeVersionDetails == null)
                    return null;

                var recordId = routeVersionDetails.RecordId;

                // Step 2-6: Execute remaining API calls in parallel using Task.WhenAll
                var tasks = new List<Task>
                {
                    _recordService.GetRecordDetailsAsync(recordId),
                    _recordService.GetRecordReductionFactorsAsync(recordId),
                    _recordService.GetActiveVesselAsync(recordId),
                    GetVoyageLegsAsync(routeVersionId),
                    GetVoyageLegReductionFactors(routeVersionId)
                };

                await Task.WhenAll(tasks);

                // Unpack results from Task.Run following the pattern
                var recordDetails = await (Task<RecordDetailDto?>)tasks[0];
                var recordReductionFactors = await (Task<RecordReductionFactorsResponse?>)tasks[1];
                var activeVessel = await (Task<ActiveVesselResponse?>)tasks[2];
                var voyageLegs = await (Task<RouteVersionLegs>)tasks[3];
                var voyageLegReductionFactors = await (Task<RouteVersionReductionFactors>)tasks[4];

                // Step 7: Initialize Data Collector and populate it
                var dataCollector = new ReportDataCollector();

                // Populate Data Collector with all the fetched data
                PopulateDataCollector(dataCollector, routeVersionDetails, recordDetails, recordReductionFactors,
                                    activeVessel, voyageLegs, voyageLegReductionFactors);

                // Step 8: Build Complete Report Response from Data Collector
                var response = BuildCompleteReportFromDataCollector(dataCollector, routeVersionId, recordId);

                return response;
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while building complete report for route version {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        private void PopulateDataCollector(ReportDataCollector dataCollector,
            RouteVersionResponse routeVersionDetails,
            RecordDetailDto? recordDetails,
            RecordReductionFactorsResponse? recordReductionFactors,
            ActiveVesselResponse? activeVessel,
            RouteVersionLegs voyageLegs,
            RouteVersionReductionFactors voyageLegReductionFactors)
        {
            // Set basic report info
            dataCollector.ReportTitle = recordDetails?.RouteName ?? string.Empty;
            dataCollector.ReportInfo.ReportDate = DateOnly.Parse(routeVersionDetails.RecordDate);
            dataCollector.ReportInfo.ReportName = recordDetails?.RouteName ?? string.Empty;

            // Set vessel info
            if (activeVessel?.Vessel != null)
            {
                dataCollector.VesselInfo.VesselName = activeVessel.Vessel.Name;
                dataCollector.VesselInfo.IMONumber = activeVessel.Vessel.Imo;
                dataCollector.VesselInfo.Flag = activeVessel.Vessel.Flag;
                dataCollector.VesselInfo.Breadth = activeVessel.Vessel.Breadth;
            }

            // Process voyage legs ONCE and populate all related data
            ProcessVoyageLegsData(dataCollector, voyageLegs, voyageLegReductionFactors, recordReductionFactors);

            // Set static notes
            dataCollector.Notes.VesselCriteria = "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.";
            dataCollector.Notes.GuideNote = "ABS Guide for Certification of Container Security Systems (April 2025)";
        }

        private void ProcessVoyageLegsData(ReportDataCollector dataCollector,
            RouteVersionLegs voyageLegs,
            RouteVersionReductionFactors voyageLegReductionFactors,
            RecordReductionFactorsResponse? recordReductionFactors)
        {
            if (voyageLegs?.VoyageLegs?.Any() != true)
                return;

            var legs = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).ToList();
            var portInfoList = new List<PortInfo>();
            var reductionFactorResults = new List<VoyageLegReductionFactor>();

            // Single loop through voyage legs to get ALL needed data
            foreach (var leg in legs)
            {
                // Build port info for route info (avoid duplicates)
                var departurePort = new PortInfo
                {
                    Name = leg.DeparturePortName,
                    Unlocode = leg.DeparturePortCode
                };
                var arrivalPort = new PortInfo
                {
                    Name = leg.ArrivalPortName,
                    Unlocode = leg.ArrivalPortCode
                };

                // Add departure port only for first leg
                if (leg.VoyageLegOrder == legs.First().VoyageLegOrder)
                {
                    portInfoList.Add(departurePort);
                }
                portInfoList.Add(arrivalPort);

                // Build reduction factor results for route analysis
                var reductionFactor = voyageLegReductionFactors.VoyageLegReductionFactors
                    .FirstOrDefault(rf => rf.VoyageLegOrder == leg.VoyageLegOrder);

                if (reductionFactor != null)
                {
                    reductionFactorResults.Add(new VoyageLegReductionFactor
                    {
                        LegOrder = leg.VoyageLegOrder,
                        DeparturePort = departurePort,
                        ArrivalPort = arrivalPort,
                        Distance = leg.Distance,
                        ReductionFactors = new ReductionFactors
                        {
                            Annual = reductionFactor.ReductionFactors?.Annual ?? 0,
                            Spring = reductionFactor.ReductionFactors?.Spring ?? 0,
                            Summer = reductionFactor.ReductionFactors?.Summer ?? 0,
                            Fall = reductionFactor.ReductionFactors?.Fall ?? 0,
                            Winter = reductionFactor.ReductionFactors?.Winter ?? 0
                        }
                    });
                }
            }

            // Set all the data in data collector
            dataCollector.RouteInfo.Ports = portInfoList;
            dataCollector.ReductionFactorResults = reductionFactorResults;

            // Set attention block data (using first and last ports from our processed data)
            if (portInfoList.Any())
            {
                dataCollector.AttentionBlock.DeparturePort = portInfoList.First().Name;
                dataCollector.AttentionBlock.ArrivalPort = portInfoList.Last().Name;
                dataCollector.AttentionBlock.ReductionFactor = recordReductionFactors?.ReductionFactors?.Annual ?? 0;
            }
        }

        private CompleteReportResponse BuildCompleteReportFromDataCollector(ReportDataCollector dataCollector, string routeVersionId, string recordId)
        {
            return new CompleteReportResponse
            {
                RouteVersionId = routeVersionId,
                RecordId = recordId,
                Report = new ReportData
                {
                    Title = dataCollector.ReportTitle,
                    DownloadTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Sections = new ReportSections
                    {
                        Attention = new AttentionSection
                        {
                            Salutation = dataCollector.AttentionBlock.Salutation,
                            Body = AttentionBlock.BuildAttentionBody(
                                dataCollector.AttentionBlock.DeparturePort,
                                dataCollector.AttentionBlock.ArrivalPort,
                                dataCollector.AttentionBlock.ReductionFactor.ToString("0.00")
                            ),
                            AbsContact = dataCollector.AttentionBlock.ABSContact
                        },
                        UserInputs = new UserInputsSection
                        {
                            ReportInfo = new ReportInfoSection
                            {
                                RouteName = dataCollector.ReportInfo.ReportName,
                                ReportDate = dataCollector.ReportInfo.ReportDate.ToString("yyyy-MM-dd")
                            },
                            Vessel = new VesselSection
                            {
                                Imo = dataCollector.VesselInfo.IMONumber,
                                Name = dataCollector.VesselInfo.VesselName,
                                Flag = dataCollector.VesselInfo.Flag
                            },
                            Ports = dataCollector.RouteInfo.Ports.Select(p => $"{p.Name} ({p.Unlocode})").ToList()
                        },
                        RouteAnalysis = dataCollector.ReductionFactorResults.Select(rf => new RouteAnalysisSegment
                        {
                            Segment = new SegmentInfo
                            {
                                Name = $"{rf.DeparturePort.Name} ({rf.DeparturePort.Unlocode}) - {rf.ArrivalPort.Name} ({rf.ArrivalPort.Unlocode})",
                                Order = rf.LegOrder,
                                Distance = rf.Distance,
                                ReductionFactors = new ReductionFactorsInfo
                                {
                                    Annual = rf.ReductionFactors.Annual,
                                    Spring = rf.ReductionFactors.Spring,
                                    Summer = rf.ReductionFactors.Summer,
                                    Fall = rf.ReductionFactors.Fall,
                                    Winter = rf.ReductionFactors.Winter
                                }
                            }
                        }).ToList(),
                        Notes = new List<ReportNote>
                        {
                            new ReportNote
                            {
                                VesselCriteria = dataCollector.Notes.VesselCriteria,
                                GuideNote = dataCollector.Notes.GuideNote
                            }
                        }
                    }
                }
            };
        }
    }
}

// 4. Updated RouteVersionsController with New Endpoint
namespace NextGenEngApps.DigitalRules.CRoute.API.Controllers
{
    [Route("api/route_versions")]
    [ApiController]
    public class RouteVersionsController : ControllerBase
    {
        private readonly ILogger<RouteVersionsController> _logger;
        private readonly IRouteVersionService _routeVersionService;

        public RouteVersionsController(ILogger<RouteVersionsController> logger, IRouteVersionService routeVersionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeVersionService = routeVersionService ?? throw new ArgumentNullException(nameof(routeVersionService));
        }

        // Existing endpoints remain unchanged...
        [HttpGet("{routeVersionId}/voyage_legs")]
        public async Task<IActionResult> GetVoyageLegs(string routeVersionId)
        {
            try
            {
                var legs = await _routeVersionService.GetVoyageLegsAsync(routeVersionId);
                return Ok(legs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error occured on GetVoyageLegs");
                return StatusCode((int)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{route_version_id}")]
        public async Task<IActionResult> GetRouteVersionDetails(string route_version_id)
        {
            try
            {
                if (string.IsNullOrEmpty(route_version_id))
                    return BadRequest("Route Version ID is required");

                var result = await _routeVersionService.GetRouteVersionDetailsAsync(route_version_id);

                if (result == null)
                    return NotFound($"Route Version with ID {route_version_id} not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetRouteVersionDetails");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{routeVersionId}/voyage_leg_reduction_factors")]
        public async Task<IActionResult> GetVoyageLegReductionFactors(string routeVersionId)
        {
            try
            {
                var rfs = await _routeVersionService.GetVoyageLegReductionFactors(routeVersionId);
                return Ok(rfs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error occured on GetVoyageLegReductionFactors");
                return StatusCode((int)StatusCodes.Status500InternalServerError);
            }
        }

        // NEW ENDPOINT: Complete Report Data
        [HttpGet("{route_version_id}/json_report")]
        public async Task<IActionResult> GetCompleteReportData(string route_version_id)
        {
            try
            {
                if (string.IsNullOrEmpty(route_version_id))
                    return BadRequest("Route Version ID is required");

                var result = await _routeVersionService.GetCompleteReportDataAsync(route_version_id);

                if (result == null)
                    return NotFound($"Complete report data not found for Route Version ID {route_version_id}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetCompleteReportData for Route Version {RouteVersionId}", route_version_id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}