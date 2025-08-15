// 1. Response Models for the Complete Report
// ResponseObjects/CompleteReportResponse.cs
using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;
using NextGenEngApps.DigitalRules.CRoute.Services.API;
using SeaRouteModel.Models;

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
        public string? VesselCriteria { get; set; }
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

        // Niranjan
        public async Task<CompleteReportResponse?> GetCompleteReportDataAsync(string routeVersionId)
        {
            try
            {
                var routeVersionDetails = await GetRouteVersionDetailsAsync(routeVersionId);
                if (routeVersionDetails == null)
                    return null;

                var recordId = routeVersionDetails.RecordId;
                var recordDetails = await _recordService.GetRecordDetailsAsync(recordId);

                var recordReductionFactors = await _recordService.GetRecordReductionFactorsAsync(recordId);
                var activeVessel = await _recordService.GetActiveVesselAsync(recordId);

                var voyageLegs = await GetVoyageLegsAsync(routeVersionId);

                var voyageLegReductionFactors = await GetVoyageLegReductionFactors(routeVersionId);

                var response = new CompleteReportResponse
                {
                    RouteVersionId = routeVersionId,
                    RecordId = recordId,
                    Report = new ReportData
                    {
                        Title = recordDetails?.RouteName ?? string.Empty,
                        DownloadTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        Sections = new ReportSections
                        {
                            Attention = BuildAttentionSection(voyageLegs, recordReductionFactors),
                            UserInputs = BuildUserInputsSection(recordDetails, activeVessel, voyageLegs, routeVersionDetails),
                            RouteAnalysis = BuildRouteAnalysisSection(voyageLegs, voyageLegReductionFactors),
                            Notes = BuildNotesSection()
                        }
                    }
                };

                return response;
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while building complete report for route version {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        private AttentionSection BuildAttentionSection(RouteVersionLegs? voyageLegs, RecordReductionFactorsResponse? reductionFactors)
        {
            var attention = new AttentionSection
            {
                Salutation = "Mr. Alan Bond, Mani Industries (WCN: 123456)",
                AbsContact = "For any clarifications, contact Mr. Holland Wright at +65 6371 2xxx or (HWright@eagle.org)."
            };

            if (voyageLegs?.VoyageLegs?.Any() == true && reductionFactors != null)
            {
                var firstLeg = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).First();
                var lastLeg = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).Last();

                var departurePort = $"{firstLeg.DeparturePortName}";
                var arrivalPort = $"{lastLeg.ArrivalPortName}";
                var reductionFactor = reductionFactors.ReductionFactors?.Annual.ToString("0.00") ?? "0.00";

                attention.Body = $"Based on your inputs in the ABS Online Reduction Factor Tool, the calculated Reduction Factor for the route from {departurePort} to {arrivalPort} is {reductionFactor}. More details can be found below.";
            }

            return attention;
        }

        private UserInputsSection BuildUserInputsSection(
            RecordDetailDto? recordDetails,
            ActiveVesselResponse? activeVessel,
            RouteVersionLegs? voyageLegs,
            RouteVersionResponse? routeVersionDetails)
        {
            var userInputs = new UserInputsSection
            {
                ReportInfo = new ReportInfoSection
                {
                    RouteName = recordDetails?.RouteName ?? string.Empty,
                    ReportDate = routeVersionDetails?.RecordDate ?? string.Empty
                },
                Vessel = new VesselSection
                {
                    Imo = activeVessel?.Vessel?.Imo ?? string.Empty,
                    Name = activeVessel?.Vessel?.Name ?? string.Empty,
                    Flag = activeVessel?.Vessel?.Flag ?? string.Empty
                }
            };

            // Build ports list (unique ports from voyage legs)
            if (voyageLegs?.VoyageLegs?.Any() == true)
            {
                var ports = new List<string>();
                var legs = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).ToList();

                // Add departure port of first leg
                if (legs.Any())
                {
                    var firstLeg = legs.First();
                    ports.Add($"{firstLeg.DeparturePortName} ({firstLeg.DeparturePortCode})");
                }

                // Add arrival ports of all legs
                foreach (var leg in legs)
                {
                    ports.Add($"{leg.ArrivalPortName} ({leg.ArrivalPortCode})");
                }

                userInputs.Ports = ports.Distinct().ToList();
            }

            return userInputs;
        }

        private List<RouteAnalysisSegment> BuildRouteAnalysisSection(
            RouteVersionLegs? voyageLegs,
            RouteVersionReductionFactors? reductionFactors)
        {
            var routeAnalysis = new List<RouteAnalysisSegment>();

            if (voyageLegs?.VoyageLegs?.Any() == true && reductionFactors?.VoyageLegReductionFactors?.Any() == true)
            {
                foreach (var leg in voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder))
                {
                    var reductionFactor = reductionFactors.VoyageLegReductionFactors
                        .FirstOrDefault(rf => rf.VoyageLegOrder == leg.VoyageLegOrder);

                    if (reductionFactor != null)
                    {
                        routeAnalysis.Add(new RouteAnalysisSegment
                        {
                            Segment = new SegmentInfo
                            {
                                Name = $"{leg.DeparturePortName} ({leg.DeparturePortCode}) - {leg.ArrivalPortName} ({leg.ArrivalPortCode})",
                                Order = leg.VoyageLegOrder,
                                Distance = leg.Distance,
                                ReductionFactors = new ReductionFactorsInfo
                                {
                                    Annual = reductionFactor.ReductionFactors?.Annual ?? 0,
                                    Spring = reductionFactor.ReductionFactors?.Spring ?? 0,
                                    Summer = reductionFactor.ReductionFactors?.Summer ?? 0,
                                    Fall = reductionFactor.ReductionFactors?.Fall ?? 0,
                                    Winter = reductionFactor.ReductionFactors?.Winter ?? 0
                                }
                            }
                        });
                    }
                }
            }

            return routeAnalysis;
        }

        private List<ReportNote> BuildNotesSection()
        {
            return new List<ReportNote>
            {
                new ReportNote
                {
                    VesselCriteria = "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors."
                },
                new ReportNote
                {
                    GuideNote = "ABS Guide for Certification of Container Security Systems (April 2025)"
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

        // Niranjan
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