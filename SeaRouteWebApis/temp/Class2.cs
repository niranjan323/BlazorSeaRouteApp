// 1. ReportController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

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

// 2. IReportService.cs
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IReportService
    {
        Task<RecordReductionFactorsDto?> GetRecordReductionFactorsAsync(string recordId);
        Task<RecordDetailsDto?> GetRecordDetailsAsync(string recordId);
        Task<RouteVersionDetailsDto?> GetRouteVersionDetailsAsync(string routeVersionId);
    }
}

// 3. ReportService.cs
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
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

        public async Task<RecordReductionFactorsDto?> GetRecordReductionFactorsAsync(string recordId)
        {
            try
            {
                // Handle case sensitivity by converting to uppercase and then parsing
                recordId = recordId.ToUpper();
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                var reductionFactors = await _reportRepository.GetRecordReductionFactorsAsync(recordGuid);

                if (reductionFactors == null)
                    return null;

                return new RecordReductionFactorsDto
                {
                    RecordId = recordId,
                    ReductionFactors = new ReductionFactorsResponse
                    {
                        Annual = reductionFactors.Annual,
                        Spring = reductionFactors.Spring,
                        Summer = reductionFactors.Summer,
                        Fall = reductionFactors.Fall,
                        Winter = reductionFactors.Winter
                    }
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<RecordDetailsDto?> GetRecordDetailsAsync(string recordId)
        {
            try
            {
                // Handle case sensitivity by converting to uppercase and then parsing
                recordId = recordId.ToUpper();
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return null;

                var record = await _reportRepository.GetRecordDetailsAsync(recordGuid);

                if (record == null)
                    return null;

                return new RecordDetailsDto
                {
                    RecordId = recordId,
                    RouteName = record.RouteName ?? string.Empty,
                    RouteDistance = record.RouteDistance ?? 0
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<RouteVersionDetailsDto?> GetRouteVersionDetailsAsync(string routeVersionId)
        {
            try
            {
                if (!Guid.TryParse(routeVersionId, out Guid routeVersionGuid))
                    return null;

                var routeVersion = await _reportRepository.GetRouteVersionDetailsAsync(routeVersionGuid);

                if (routeVersion == null)
                    return null;

                return new RouteVersionDetailsDto
                {
                    RouteVersionId = routeVersionGuid.ToString(),
                    RecordDate = routeVersion.RecordDate?.ToString("yyyy-MM-dd") ?? string.Empty
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

// 4. IReportRepository.cs
using NextGenEngApps.DigitalRules.CRoute.API.Models;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public interface IReportRepository
    {
        Task<ReductionFactorsInfo?> GetRecordReductionFactorsAsync(Guid recordId);
        Task<RecordBasicInfo?> GetRecordDetailsAsync(Guid recordId);
        Task<RouteVersionInfo?> GetRouteVersionDetailsAsync(Guid routeVersionId);
    }
}

// 5. ReportRepository.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
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

        public async Task<ReductionFactorsInfo?> GetRecordReductionFactorsAsync(Guid recordId)
        {
            try
            {
                var reductionFactors = await (from rrf in _dbContext.RecordReductionFactors
                                              join st in _dbContext.SeasonTypes on rrf.SeasonType equals st.SeasonTypeId
                                              where rrf.RecordId == recordId && rrf.IsActive == true
                                              select new { st.SeasonName, rrf.ReductionFactor })
                                              .ToListAsync();

                if (!reductionFactors.Any())
                    return null;

                var result = new ReductionFactorsInfo();

                foreach (var factor in reductionFactors)
                {
                    switch (factor.SeasonName.ToLower())
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

        public async Task<RecordBasicInfo?> GetRecordDetailsAsync(Guid recordId)
        {
            try
            {
                var record = await _dbContext.Records
                    .Where(r => r.RecordId == recordId && r.IsActive == true)
                    .Select(r => new RecordBasicInfo
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

        public async Task<RouteVersionInfo?> GetRouteVersionDetailsAsync(Guid routeVersionId)
        {
            try
            {
                var routeVersion = await _dbContext.RouteVersions
                    .Where(rv => rv.RouteVersionId == routeVersionId && rv.IsActive == true)
                    .Select(rv => new RouteVersionInfo
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

// 6. DTOs (if not already created) - Add to your DTOs folder

// RecordReductionFactorsDto.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class RecordReductionFactorsDto
    {
        public string RecordId { get; set; } = string.Empty;
        public ReductionFactorsResponse ReductionFactors { get; set; } = new();
    }
}

// ReductionFactorsResponse.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
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

// RouteVersionDetailsDto.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class RouteVersionDetailsDto
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordDate { get; set; } = string.Empty;
    }
}

// 7. Models (Add to your Models folder)

// ReductionFactorsInfo.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Models
{
    public class ReductionFactorsInfo
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }
}

// RecordBasicInfo.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Models
{
    public class RecordBasicInfo
    {
        public string? RouteName { get; set; }
        public double? RouteDistance { get; set; }
    }
}

// RouteVersionInfo.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Models
{
    public class RouteVersionInfo
    {
        public DateTime? RecordDate { get; set; }
    }
}

// 8. Don't forget to register in Program.cs or Startup.cs
// Add these lines to your dependency injection container:
// builder.Services.AddScoped<IReportService, ReportService>();
// builder.Services.AddScoped<IReportRepository, ReportRepository>();