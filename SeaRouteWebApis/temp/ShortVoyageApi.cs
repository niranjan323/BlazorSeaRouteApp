// 1. DTO for Edit Response
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.API.Services;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.Models;
using SeaRouteWebApis.Interfaces;
using SeaRouteWebApis.Services;

namespace NextGenEngApps.DigitalRules.CRoute.Models
{
    public class EditShortVoyageReductionFactorDto
    {
        public Guid RecordId { get; set; }
        public string RouteName { get; set; }
        public string VesselName { get; set; }
        public string IMONo { get; set; }
        public int Breadth { get; set; }
        public string PortOfDeparture { get; set; }
        public string PortOfArrival { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ForecastTime { get; set; }
        public double? ForecastHswell { get; set; }
        public double? ForecastHwind { get; set; }
        public string TimeZone { get; set; } = "UTC";
        public int? Duration { get; set; }
        public decimal? ReductionFactor { get; set; }
    }
}

// 2. Service Interface
namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IShortVoyageService
    {
        Task<EditShortVoyageReductionFactorDto> GetShortVoyageRecordForEditAsync(Guid recordId);
    }
}

// 3. Service Implementation
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.Models;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class ShortVoyageService : IShortVoyageService
    {
        private readonly IShortVoyageRepository _shortVoyageRepository;
        private readonly ILogger<ShortVoyageService> _logger;

        public ShortVoyageService(
            IShortVoyageRepository shortVoyageRepository,
            ILogger<ShortVoyageService> logger)
        {
            _shortVoyageRepository = shortVoyageRepository ?? throw new ArgumentNullException(nameof(shortVoyageRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EditShortVoyageReductionFactorDto> GetShortVoyageRecordForEditAsync(Guid recordId)
        {
            try
            {
                _logger.LogInformation($"Getting short voyage record for edit with ID: {recordId}");

                var record = await _shortVoyageRepository.GetShortVoyageRecordByIdAsync(recordId);
                if (record == null)
                {
                    _logger.LogWarning($"Short voyage record not found for ID: {recordId}");
                    return null;
                }

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting short voyage record for edit with ID: {recordId}");
                throw;
            }
        }
    }
}

// 4. Repository Interface
namespace NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces
{
    public interface IShortVoyageRepository
    {
        Task<EditShortVoyageReductionFactorDto> GetShortVoyageRecordByIdAsync(Guid recordId);
    }
}

// 5. Repository Implementation
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.Models;

namespace NextGenEngApps.DigitalRules.CRoute.API.Repositories
{
    public class ShortVoyageRepository : IShortVoyageRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ShortVoyageRepository> _logger;

        public ShortVoyageRepository(ApplicationDbContext dbContext, ILogger<ShortVoyageRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EditShortVoyageReductionFactorDto> GetShortVoyageRecordByIdAsync(Guid recordId)
        {
            try
            {
                var result = await (from svr in _dbContext.ShortVoyageRecords
                                    join r in _dbContext.Records on svr.RecordId equals r.RecordId
                                    where svr.RecordId == recordId && svr.IsActive == true
                                    select new EditShortVoyageReductionFactorDto
                                    {
                                        RecordId = svr.RecordId,
                                        RouteName = r.RouteName,
                                        DepartureTime = svr.DepartureTime,
                                        ArrivalTime = svr.ArrivalTime,
                                        ForecastTime = svr.ForecastTime,
                                        ForecastHswell = svr.ForecastHswell,
                                        ForecastHwind = svr.ForecastHwind,
                                        TimeZone = "UTC",
                                        Duration = (int?)((svr.ArrivalTime - svr.DepartureTime).TotalHours),
                                        // Note:  need to add joins for vessel and port data
                                        
                                        VesselName = "", 
                                        IMONo = "", 
                                        Breadth = 0,
                                        PortOfDeparture = "", 
                                        PortOfArrival = "" 
                                    }).FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving short voyage record with ID: {recordId}");
                throw;
            }
        }
    }
}

// 6. Controller - Add this method to your existing ShortVoyageRecordsController
[HttpGet("{id}/edit")]
public async Task<IActionResult> GetShortVoyageRecordForEdit(string id)
{
    try
    {
        if (!Guid.TryParse(id, out Guid recordGuid))
        {
            return BadRequest("Invalid record ID format.");
        }

        var editRecord = await _shortVoyageService.GetShortVoyageRecordForEditAsync(recordGuid);

        if (editRecord == null)
        {
            return NotFound($"Short voyage record not found with ID: {id}");
        }

        return Ok(editRecord);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred on GetShortVoyageRecordForEdit");
        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the record.");
    }
}

// 7. Updated Controller Constructor - Add IShortVoyageService dependency
private readonly IShortVoyageService _shortVoyageService;

public ShortVoyageRecordsController(
    ILoggerFactory loggerFactory,
    IRepository<ShortVoyageRecord> repository,
    IShortVoyageService shortVoyageService) : base(loggerFactory, repository)
{
    _shortVoyageService = shortVoyageService ?? throw new ArgumentNullException(nameof(shortVoyageService));
}
