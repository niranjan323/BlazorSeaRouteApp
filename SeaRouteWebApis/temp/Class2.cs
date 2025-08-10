// Modified by Niranjan - Architecture fixes based on team lead feedback

// 1. Create Response Objects folder and classes

// ResponseObjects/RecordReductionFactorsResponse.cs
// Modified by Niranjan - Renamed from RecordReductionFactorsDto and moved to ResponseObjects folder
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class RecordReductionFactorsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public ReductionFactorsResponse ReductionFactors { get; set; } = new();
    }
}

// ResponseObjects/ReductionFactorsResponse.cs
// Modified by Niranjan - Moved from DTOs folder to ResponseObjects folder
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class ReductionFactorsResponse
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }
}

// ResponseObjects/RecordDetailsResponse.cs
// Modified by Niranjan - Renamed from RecordDetailsDto and moved to ResponseObjects folder
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class RecordDetailsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public double RouteDistance { get; set; }
    }
}

// ResponseObjects/RouteVersionDetailsResponse.cs
// Modified by Niranjan - Created new response object for route version endpoint
namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class RouteVersionDetailsResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordDate { get; set; } = string.Empty;
    }
}

// 2. Create DTOs for Repository layer

// Dtos/ReductionFactorsDto.cs
// Modified by Niranjan - Renamed from ReductionFactorsInfo and moved to DTOs folder for repository layer
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class ReductionFactorsDto
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }
}

// Dtos/RecordBasicDto.cs
// Modified by Niranjan - Renamed from RecordBasicInfo and moved to DTOs folder for repository layer
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class RecordBasicDto
    {
        public string? RouteName { get; set; }
        public double? RouteDistance { get; set; }
    }
}

// Dtos/RouteVersionDto.cs
// Modified by Niranjan - Renamed from RouteVersionInfo and moved to DTOs folder for repository layer
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class RouteVersionDto
    {
        public DateTime? RecordDate { get; set; }
    }
}

// 3. Updated IReportService interface
// Modified by Niranjan - Changed return types from DTOs to Response Objects
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IReportService
    {
        Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId);
        Task<RecordDetailsResponse?> GetRecordDetailsAsync(string recordId);
        Task<RouteVersionDetailsResponse?> GetRouteVersionDetailsAsync(string routeVersionId);
    }
}

// 4. Updated ReportService
// Modified by Niranjan - Changed to return Response Objects instead of DTOs, proper architecture separation
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

        public async Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId)
        {
            try
            {
                // Modified by Niranjan - Removed .ToUpper() as SQL Server GUIDs are case-insensitive
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                // Modified by Niranjan - Now repository returns DTO, service converts to Response Object
                var reductionFactorsDto = await _reportRepository.GetRecordReductionFactorsAsync(recordGuid);

                if (reductionFactorsDto == null)
                    return null;

                // Modified by Niranjan - Service layer converts DTO to Response Object
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
                // Modified by Niranjan - Removed .ToUpper() as SQL Server GUIDs are case-insensitive
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                // Modified by Niranjan - Repository now returns DTO instead of Info model
                var recordDto = await _reportRepository.GetRecordDetailsAsync(recordGuid);

                if (recordDto == null)
                    return null;

                // Modified by Niranjan - Convert DTO to Response Object
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
                // Modified by Niranjan - No case conversion needed for SQL Server GUIDs
                if (!Guid.TryParse(routeVersionId, out Guid routeVersionGuid))
                    return null;

                // Modified by Niranjan - Repository returns DTO, service converts to Response Object
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
    }
}

// 5. Updated IReportRepository interface
// Modified by Niranjan - Changed return types from Info models to DTOs as requested by team lead
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public interface IReportRepository
    {
        Task<ReductionFactorsDto?> GetRecordReductionFactorsAsync(Guid recordId);
        Task<RecordBasicDto?> GetRecordDetailsAsync(Guid recordId);
        Task<RouteVersionDto?> GetRouteVersionDetailsAsync(Guid routeVersionId);
    }
}

// 6. Updated ReportRepository
// Modified by Niranjan - Repository now returns DTOs instead of Info models, fixed architecture
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

        public async Task<ReductionFactorsDto?> GetRecordReductionFactorsAsync(Guid recordId)
        {
            try
            {
                // Modified by Niranjan - Added join with season_types table as requested
                var reductionFactors = await (from rrf in _dbContext.RecordReductionFactors
                                              join st in _dbContext.SeasonTypes on rrf.SeasonType equals st.SeasonType1
                                              where rrf.RecordId == recordId && rrf.IsActive == true
                                              select new { st.SeasonName, rrf.ReductionFactor })
                                              .ToListAsync();

                if (!reductionFactors.Any())
                    return null;

                // Modified by Niranjan - Return DTO instead of Info model
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
                // Modified by Niranjan - Return DTO instead of Info model
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
                // Modified by Niranjan - Return DTO instead of Info model
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
    }
}

// 7. Updated ReportController (no changes needed, just using Response Objects now)
// Modified by Niranjan - Controller now receives Response Objects from service layer
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
    }
}