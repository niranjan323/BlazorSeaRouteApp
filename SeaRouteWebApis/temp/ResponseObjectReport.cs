// 1. Response Object for Save Report
// ResponseObjects/SaveReportResponse.cs
using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.API.RequestObjects;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects
{
    public class SaveReportResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public int RecordRouteVersion { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

// 2. Request Object for Save Report
// RequestObjects/SaveReportRequest.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.RequestObjects
{
    public class SaveReportRequest
    {
        public string RecordId { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? RecordDate { get; set; }
        // Add other report data properties as needed
        // public List<VoyageLegData> VoyageLegs { get; set; } = new();
        // public List<ReductionFactorData> ReductionFactors { get; set; } = new();
    }
}

// 3. DTO for Repository layer
// Dtos/SaveReportDto.cs
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class SaveReportDto
    {
        public Guid RecordId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? RecordDate { get; set; }
    }

    public class RouteVersionResultDto
    {
        public Guid RouteVersionId { get; set; }
        public Guid RecordId { get; set; }
        public int RecordRouteVersion { get; set; }
    }
}

// 4. Updated IRouteVersionService interface
using NextGenEngApps.DigitalRules.CRoute.API.RequestObjects;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IRouteVersionService
    {
        // Existing method
        Task<object> GetVoyageLegsAsync(string routeVersionId);

        // New method for saving report
        Task<SaveReportResponse> SaveReportAsync(SaveReportRequest request);
    }
}

// 5. Updated RouteVersionService
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.RequestObjects;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class RouteVersionService : IRouteVersionService
    {
        private readonly IRouteVersionRepository _routeVersionRepository;
        private readonly ILogger<RouteVersionService> _logger;

        public RouteVersionService(IRouteVersionRepository routeVersionRepository, ILogger<RouteVersionService> logger)
        {
            _routeVersionRepository = routeVersionRepository ?? throw new ArgumentNullException(nameof(routeVersionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> GetVoyageLegsAsync(string routeVersionId)
        {
            // Existing implementation
            throw new NotImplementedException();
        }

        public async Task<SaveReportResponse> SaveReportAsync(SaveReportRequest request)
        {
            try
            {
                if (!Guid.TryParse(request.RecordId, out Guid recordGuid))
                    throw new ArgumentException("Invalid Record ID format");

                var saveDto = new SaveReportDto
                {
                    RecordId = recordGuid,
                    CreatedBy = request.CreatedBy,
                    RecordDate = request.RecordDate
                };

                // Repository handles the complex logic of finding highest version, 
                // setting is_active to 0, and creating new route version
                var result = await _routeVersionRepository.SaveReportAsync(saveDto);

                if (result == null)
                    throw new InvalidOperationException("Failed to save report data");

                return new SaveReportResponse
                {
                    RouteVersionId = result.RouteVersionId.ToString(),
                    RecordId = result.RecordId.ToString(),
                    RecordRouteVersion = result.RecordRouteVersion,
                    Message = "Report saved successfully"
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

// 6. Updated IRouteVersionRepository interface
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public interface IRouteVersionRepository
    {
        // Existing method
        Task<object> GetVoyageLegsAsync(Guid routeVersionId);

        // New method for saving report
        Task<RouteVersionResultDto> SaveReportAsync(SaveReportDto saveReportDto);
    }
}

// 7. Updated RouteVersionRepository
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public class RouteVersionRepository : IRouteVersionRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RouteVersionRepository> _logger;

        public RouteVersionRepository(ApplicationDbContext dbContext, ILogger<RouteVersionRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<object> GetVoyageLegsAsync(Guid routeVersionId)
        {
            // Existing implementation
            throw new NotImplementedException();
        }

        public async Task<RouteVersionResultDto> SaveReportAsync(SaveReportDto saveReportDto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Find the highest existing record_route_version for this record_id
                var highestVersion = await _dbContext.RouteVersions
                    .Where(rv => rv.RecordId == saveReportDto.RecordId)
                    .MaxAsync(rv => (int?)rv.RecordRouteVersion) ?? 0;

                // Step 2: Set is_active to 0 for the current active version (highest version)
                if (highestVersion > 0)
                {
                    var currentActiveVersion = await _dbContext.RouteVersions
                        .FirstOrDefaultAsync(rv => rv.RecordId == saveReportDto.RecordId &&
                                                  rv.RecordRouteVersion == highestVersion &&
                                                  rv.IsActive == true);

                    if (currentActiveVersion != null)
                    {
                        currentActiveVersion.IsActive = false;
                        currentActiveVersion.ModifiedDate = DateTime.UtcNow;
                        currentActiveVersion.ModifiedBy = saveReportDto.CreatedBy;
                    }
                }

                // Step 3: Create new route version entry
                var newRouteVersion = new RouteVersion
                {
                    RouteVersionId = Guid.NewGuid(),
                    RecordId = saveReportDto.RecordId,
                    RecordRouteVersion = highestVersion, // Use same version number, not increment
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = saveReportDto.CreatedBy,
                    IsActive = false, // Set to 0 as per requirement (implicit save)
                    RecordDate = saveReportDto.RecordDate?.Date
                };

                _dbContext.RouteVersions.Add(newRouteVersion);

                // Step 4: Save all changes
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully saved report for record {saveReportDto.RecordId} with route version {newRouteVersion.RouteVersionId}");

                return new RouteVersionResultDto
                {
                    RouteVersionId = newRouteVersion.RouteVersionId,
                    RecordId = newRouteVersion.RecordId,
                    RecordRouteVersion = newRouteVersion.RecordRouteVersion
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"An error occurred while saving report for record {saveReportDto.RecordId}: {ex.Message}");
                throw;
            }
        }
    }
}

// 8. Updated RouteVersionsController
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.RequestObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;

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

        [HttpPost("save_report")]
        public async Task<IActionResult> SaveReport([FromBody] SaveReportRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request body is required");

                if (string.IsNullOrEmpty(request.RecordId))
                    return BadRequest("Record ID is required");

                if (string.IsNullOrEmpty(request.CreatedBy))
                    return BadRequest("Created By is required");

                var result = await _routeVersionService.SaveReportAsync(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters for SaveReport");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred on SaveReport");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while saving the report");
            }
        }
    }
}

// 9. RouteVersion Entity Model (if not exists)
// Models/RouteVersion.cs
namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models
{
    public class RouteVersion
    {
        public Guid RouteVersionId { get; set; }
        public Guid RecordId { get; set; }
        public int RecordRouteVersion { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime? RecordDate { get; set; }

        // Navigation property
        public virtual Record? Record { get; set; }
    }
}