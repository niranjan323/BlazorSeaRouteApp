// 1. Response Objects
// ResponseObjects/ActiveVesselResponse.cs
using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class ActiveVesselResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public VesselResponse Vessel { get; set; } = new();
    }
}

// ResponseObjects/VesselResponse.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class VesselResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Imo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public double Breadth { get; set; }
    }
}

// 2. DTOs for Repository layer
// Dtos/ActiveVesselDto.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class ActiveVesselDto
    {
        public Guid VesselId { get; set; }
        public string? VesselImo { get; set; }
        public string? VesselName { get; set; }
        public string? Flag { get; set; }
        public double? VesselBreadth { get; set; }
    }
}

// 3. Updated IReportService interface
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IReportService
    {
        // Existing methods
        Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId);
        Task<RecordDetailsResponse?> GetRecordDetailsAsync(string recordId);
        Task<RouteVersionDetailsResponse?> GetRouteVersionDetailsAsync(string routeVersionId);

        // New method for active vessel
        Task<ActiveVesselResponse?> GetActiveVesselAsync(string recordId);
    }
}

// 4. Updated ReportService
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IReportRepository reportRepository, ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Existing methods
        public async Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                var reductionFactorsDto = await _reportRepository.GetRecordReductionFactorsAsync(recordGuid);

                if (reductionFactorsDto == null)
                    return null;

                return new RecordReductionFactorsResponse
                {
                    RecordId = recordGuid.ToString(),
                    ReductionFactors = new ReductionFactorsResponse
                    {
                        Annual = reductionFactorsDto.Annual,
                        Spring = reductionFactorsDto.Spring,
                        Summer = reductionFactorsDto.Summer,
                        Fall = reductionFactorsDto.Fall,
                        Winter = reductionFactorsDto.Winter
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<RecordDetailsResponse?> GetRecordDetailsAsync(string recordId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                var recordDto = await _reportRepository.GetRecordDetailsAsync(recordGuid);

                if (recordDto == null)
                    return null;

                return new RecordDetailsResponse
                {
                    RecordId = recordGuid.ToString(),
                    RouteName = recordDto.RouteName ?? string.Empty,
                    RouteDistance = recordDto.RouteDistance ?? 0
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<RouteVersionDetailsResponse?> GetRouteVersionDetailsAsync(string routeVersionId)
        {
            try
            {
                if (!Guid.TryParse(routeVersionId, out Guid routeVersionGuid))
                    return null;

                var routeVersionDto = await _reportRepository.GetRouteVersionDetailsAsync(routeVersionGuid);

                if (routeVersionDto == null)
                    return null;

                return new RouteVersionDetailsResponse
                {
                    RouteVersionId = routeVersionGuid.ToString(),
                    RecordDate = routeVersionDto.RecordDate?.ToString("yyyy-MM-dd") ?? string.Empty
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        // New method for active vessel
        public async Task<ActiveVesselResponse?> GetActiveVesselAsync(string recordId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                var activeVesselDto = await _reportRepository.GetActiveVesselAsync(recordGuid);

                if (activeVesselDto == null)
                    return null;

                return new ActiveVesselResponse
                {
                    RecordId = recordGuid.ToString(),
                    Vessel = new VesselResponse
                    {
                        Id = activeVesselDto.VesselId.ToString(),
                        Imo = activeVesselDto.VesselImo ?? string.Empty,
                        Name = activeVesselDto.VesselName ?? string.Empty,
                        Flag = activeVesselDto.Flag ?? string.Empty,
                        Breadth = activeVesselDto.VesselBreadth ?? 0
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

// 5. Updated IReportRepository interface
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public interface IReportRepository
    {
        // Existing methods
        Task<ReductionFactorsDto?> GetRecordReductionFactorsAsync(Guid recordId);
        Task<RecordBasicDto?> GetRecordDetailsAsync(Guid recordId);
        Task<RouteVersionDto?> GetRouteVersionDetailsAsync(Guid routeVersionId);

        // New method for active vessel
        Task<ActiveVesselDto?> GetActiveVesselAsync(Guid recordId);
    }
}

// 6. Updated ReportRepository
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ReportRepository> _logger;

        public ReportRepository(ApplicationDbContext dbContext, ILogger<ReportRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Existing methods
        public async Task<ReductionFactorsDto?> GetRecordReductionFactorsAsync(Guid recordId)
        {
            try
            {
                var reductionFactors = await (from rrf in _dbContext.RecordReductionFactors
                                              join st in _dbContext.SeasonTypes on rrf.SeasonType equals st.SeasonType1
                                              where rrf.RecordId == recordId && rrf.IsActive == true
                                              select new { st.SeasonName, rrf.ReductionFactor })
                                              .ToListAsync();

                if (!reductionFactors.Any())
                    return null;

                var result = new ReductionFactorsDto();

                foreach (var factor in reductionFactors)
                {
                    switch (factor.SeasonName.ToLower().Trim())
                    {
                        case "annual":
                            result.Annual = factor.ReductionFactor;
                            break;
                        case "spring":
                            result.Spring = factor.ReductionFactor;
                            break;
                        case "summer":
                            result.Summer = factor.ReductionFactor;
                            break;
                        case "fall":
                            result.Fall = factor.ReductionFactor;
                            break;
                        case "winter":
                            result.Winter = factor.ReductionFactor;
                            break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching reduction factors for record {recordId}: {ex.Message}");
                throw;
            }
        }

        public async Task<RecordBasicDto?> GetRecordDetailsAsync(Guid recordId)
        {
            try
            {
                var record = await _dbContext.Records
                    .Where(r => r.RecordId == recordId && r.IsActive == true)
                    .Select(r => new RecordBasicDto
                    {
                        RouteName = r.RouteName,
                        RouteDistance = r.RouteDistance
                    })
                    .FirstOrDefaultAsync();

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching record details for record {recordId}: {ex.Message}");
                throw;
            }
        }

        public async Task<RouteVersionDto?> GetRouteVersionDetailsAsync(Guid routeVersionId)
        {
            try
            {
                var routeVersion = await _dbContext.RouteVersions
                    .Where(rv => rv.RouteVersionId == routeVersionId && rv.IsActive == true)
                    .Select(rv => new RouteVersionDto
                    {
                        RecordDate = rv.RecordDate
                    })
                    .FirstOrDefaultAsync();

                return routeVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching route version details for route version {routeVersionId}: {ex.Message}");
                throw;
            }
        }

        // New method for active vessel
        public async Task<ActiveVesselDto?> GetActiveVesselAsync(Guid recordId)
        {
            try
            {
                // Implementation based on your SQL query:
                // SELECT v.vessel_id, v.vessel_imo, v.vessel_name, v.flag, v.vessel_breadth 
                // FROM record_vessels rv 
                // JOIN vessels v ON rv.vessel_id = v.vessel_id 
                // WHERE rv.record_id = 'recordId' AND rv.is_active = 1

                var activeVessel = await (from rv in _dbContext.RecordVessels
                                          join v in _dbContext.Vessels on rv.VesselId equals v.VesselId
                                          where rv.RecordId == recordId && rv.IsActive == true
                                          select new ActiveVesselDto
                                          {
                                              VesselId = v.VesselId,
                                              VesselImo = v.VesselImo,
                                              VesselName = v.VesselName,
                                              Flag = v.Flag,
                                              VesselBreadth = v.VesselBreadth
                                          })
                                         .FirstOrDefaultAsync();

                return activeVessel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching active vessel for record {recordId}: {ex.Message}");
                throw;
            }
        }
    }
}

// 7. Updated ReportController
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;

namespace NextGenEngApps.DigitalRules.CRoute.API.Controllers
{
    [Route("api/v1/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IReportService _reportService;

        public ReportController(ILogger<ReportController> logger, IReportService reportService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        // Existing endpoints
        [HttpGet("record_reduction_factors/{id}")]
        public async Task<IActionResult> GetRecordReductionFactors(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Record ID is required");

                var result = await _reportService.GetRecordReductionFactorsAsync(id);

                if (result == null)
                    return NotFound($"Record with ID {id} not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetRecordReductionFactors");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("records/{id}")]
        public async Task<IActionResult> GetRecordDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Record ID is required");

                var result = await _reportService.GetRecordDetailsAsync(id);

                if (result == null)
                    return NotFound($"Record with ID {id} not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetRecordDetails");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("route_versions/{id}")]
        public async Task<IActionResult> GetRouteVersionDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Route Version ID is required");

                var result = await _reportService.GetRouteVersionDetailsAsync(id);

                if (result == null)
                    return NotFound($"Route Version with ID {id} not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetRouteVersionDetails");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        // New endpoint for active vessel
        [HttpGet("records/{id}/active_vessel")]
        public async Task<IActionResult> GetActiveVessel(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Record ID is required");

                var result = await _reportService.GetActiveVesselAsync(id);

                if (result == null)
                    return NotFound($"No active vessel found for record with ID {id}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on GetActiveVessel");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

// 8. Entity Models (if they don't exist)
// Models/RecordVessel.cs
namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models
{
    public class RecordVessel
    {
        public Guid RecordVesselId { get; set; }
        public Guid RecordId { get; set; }
        public Guid VesselId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public virtual Record? Record { get; set; }
        public virtual Vessel? Vessel { get; set; }
    }
}

// Models/Vessel.cs
namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models
{
    public class Vessel
    {
        public Guid VesselId { get; set; }
        public string? VesselImo { get; set; }
        public string? VesselName { get; set; }
        public string? Flag { get; set; }
        public double? VesselBreadth { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<RecordVessel> RecordVessels { get; set; } = new List<RecordVessel>();
    }
}
