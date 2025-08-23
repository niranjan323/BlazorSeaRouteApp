
namespace NextGenEngApps.DigitalRules.CRoute.Models
{
    public class ShortVoyageRecordRestoreDto
    {
        public Guid RecordId { get; set; }
        public string RouteName { get; set; }
        public decimal ReductionFactor { get; set; }
        public double RouteDistance { get; set; }
        public List<RoutePointDto> RoutePoints { get; set; } = new List<RoutePointDto>();
        public VesselDto Vessel { get; set; }
        public DateTime RecordDate { get; set; }
        public ShortVoyageDto ShortVoyage { get; set; }
    }

    public class RoutePointDto
    {
        public Guid GeoPointId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string RoutePointType { get; set; }
        public int RoutePointOrder { get; set; }
        public PortDataDto PortData { get; set; }
    }

    public class PortDataDto
    {
        public string PortCode { get; set; }
        public string PortName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }

    public class VesselDto
    {
        public string VesselName { get; set; }
        public string ImoNumber { get; set; }
        public string Flag { get; set; }
        public double Breadth { get; set; }
        public DateTime? ReportDate { get; set; }
    }

    public class ShortVoyageDto
    {
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ForecastTime { get; set; }
        public double? ForecastSwellHeight { get; set; }
        public double? ForecastWindHeight { get; set; }
        public double? ReductionFactor { get; set; }
    }
}

// 2. Service Interface
namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IShortVoyageRecordService
    {
        Task<ShortVoyageRecordRestoreDto> RestoreShortVoyageRecordAsync(Guid recordId);
    }
}

// 3. Service Implementation
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.Models;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class ShortVoyageRecordService : IShortVoyageRecordService
    {
        private readonly IShortVoyageRecordRepository _shortVoyageRecordRepository;
        private readonly ILogger<ShortVoyageRecordService> _logger;

        public ShortVoyageRecordService(
            IShortVoyageRecordRepository shortVoyageRecordRepository,
            ILogger<ShortVoyageRecordService> logger)
        {
            _shortVoyageRecordRepository = shortVoyageRecordRepository ?? throw new ArgumentNullException(nameof(shortVoyageRecordRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ShortVoyageRecordRestoreDto> RestoreShortVoyageRecordAsync(Guid recordId)
        {
            try
            {
                _logger.LogInformation($"Restoring short voyage record with ID: {recordId}");

                var record = await _shortVoyageRecordRepository.GetShortVoyageRecordForRestoreAsync(recordId);
                if (record == null)
                {
                    _logger.LogWarning($"Short voyage record not found for ID: {recordId}");
                    return null;
                }

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring short voyage record with ID: {recordId}");
                throw;
            }
        }
    }
}

// 4. Repository Interface
namespace NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces
{
    public interface IShortVoyageRecordRepository
    {
        Task<ShortVoyageRecordRestoreDto> GetShortVoyageRecordForRestoreAsync(Guid recordId);
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
    public class ShortVoyageRecordRepository : IShortVoyageRecordRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ShortVoyageRecordRepository> _logger;

        public ShortVoyageRecordRepository(ApplicationDbContext dbContext, ILogger<ShortVoyageRecordRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ShortVoyageRecordRestoreDto> GetShortVoyageRecordForRestoreAsync(Guid recordId)
        {
            try
            {
                // Get the main record with short voyage data
                var mainRecord = await (from r in _dbContext.Records
                                        join svr in _dbContext.ShortVoyageRecords on r.RecordId equals svr.RecordId
                                        where r.RecordId == recordId && r.IsActive == true && svr.IsActive == true
                                        select new
                                        {
                                            Record = r,
                                            ShortVoyage = svr
                                        }).FirstOrDefaultAsync();

                if (mainRecord == null)
                {
                    return null;
                }

                // Get route points with port data
                var routePoints = await (from rv in _dbContext.RouteVersions
                                         join rp in _dbContext.RoutePoints on rv.RouteVersionId equals rp.RouteVersionId
                                         join gp in _dbContext.GeoPoints on rp.GeoPointId equals gp.GeoPointId
                                         join p in _dbContext.Ports on gp.GeoPointId equals p.GeoPointId into portGroup
                                         from port in portGroup.DefaultIfEmpty()
                                         join c in _dbContext.Countries on port.CountryCode equals c.CountryCode into countryGroup
                                         from country in countryGroup.DefaultIfEmpty()
                                         where rv.RecordId == recordId && rv.IsActive == true && rp.IsActive == true && gp.IsActive == true
                                         orderby rp.RoutePointOrder
                                         select new RoutePointDto
                                         {
                                             GeoPointId = gp.GeoPointId,
                                             Latitude = gp.Latitude,
                                             Longitude = gp.Longitude,
                                             RoutePointType = port != null ? "port" : "waypoint",
                                             RoutePointOrder = rp.RoutePointOrder,
                                             PortData = port != null ? new PortDataDto
                                             {
                                                 PortCode = port.Unlocode,
                                                 PortName = port.PortName,
                                                 CountryCode = port.CountryCode,
                                                 CountryName = country.CountryName
                                             } : null
                                         }).ToListAsync();

                // Get vessel data
                var vesselData = await (from rv in _dbContext.RecordVessels
                                        join v in _dbContext.Vessels on rv.VesselId equals v.VesselId
                                        where rv.RecordId == recordId && rv.IsActive == true && v.IsActive == true
                                        select new VesselDto
                                        {
                                            VesselName = v.VesselName,
                                            ImoNumber = v.VesselImo,
                                            Flag = v.Flag,
                                            Breadth = v.VesselBreadth ?? 0,
                                            ReportDate = null // Set based on your business logic
                                        }).FirstOrDefaultAsync();

                // Get reduction factor (assuming it's stored in record_reduction_factors table)
                var reductionFactor = await (from rrf in _dbContext.RecordReductionFactors
                                             where rrf.RecordId == recordId && rrf.IsActive == true
                                             select rrf.ReductionFactor).FirstOrDefaultAsync();

                // Calculate short voyage reduction factor based on the existing logic
                var shortVoyageReductionFactor = CalculateShortVoyageReductionFactor(
                    mainRecord.ShortVoyage.ForecastHswell,
                    mainRecord.ShortVoyage.ForecastHwind,
                    vesselData?.Breadth ?? 0);

                var result = new ShortVoyageRecordRestoreDto
                {
                    RecordId = mainRecord.Record.RecordId,
                    RouteName = mainRecord.Record.RouteName,
                    ReductionFactor = reductionFactor,
                    RouteDistance = mainRecord.Record.RouteDistance ?? 0,
                    RoutePoints = routePoints,
                    Vessel = vesselData ?? new VesselDto(),
                    RecordDate = mainRecord.Record.CreatedDate,
                    ShortVoyage = new ShortVoyageDto
                    {
                        DepartureTime = mainRecord.ShortVoyage.DepartureTime,
                        ArrivalTime = mainRecord.ShortVoyage.ArrivalTime,
                        ForecastTime = mainRecord.ShortVoyage.ForecastTime,
                        ForecastSwellHeight = mainRecord.ShortVoyage.ForecastHswell,
                        ForecastWindHeight = mainRecord.ShortVoyage.ForecastHwind,
                        ReductionFactor = shortVoyageReductionFactor
                    }
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving short voyage record for restore with ID: {recordId}");
                throw;
            }
        }

        private double? CalculateShortVoyageReductionFactor(double? forecastHswell, double? forecastHwind, double breadth)
        {
            if (!forecastHswell.HasValue || !forecastHwind.HasValue || breadth <= 0)
                return null;

            // Calculate WaveHsmax using the same logic as in your controller
            var waveHsmax = Math.Sqrt((forecastHswell.Value * forecastHswell.Value) + (forecastHwind.Value * forecastHwind.Value));

            // Calculate reduction factor using the same logic
            var reductionFactor = Math.Max(Math.Min(waveHsmax / (2 * Math.Sqrt(breadth)) + 0.4, 1), 0.6);

            return Math.Round(reductionFactor, 3);
        }
    }
}

// 6. Controller Method - Add this to your existing ShortVoyageRecordsController
[HttpGet("short_voyage_records/{record_id}/restore")]
public async Task<IActionResult> RestoreShortVoyageRecord(string record_id)
{
    try
    {
        if (!Guid.TryParse(record_id, out Guid recordGuid))
        {
            return BadRequest("Invalid record ID format.");
        }

        var restoredRecord = await _shortVoyageRecordService.RestoreShortVoyageRecordAsync(recordGuid);

        if (restoredRecord == null)
        {
            return NotFound($"Short voyage record not found with ID: {record_id}");
        }

        return Ok(restoredRecord);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred on RestoreShortVoyageRecord");
        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while restoring the record.");
    }
}

// 7. Updated Controller Constructor - Add IShortVoyageRecordService dependency
private readonly IShortVoyageRecordService _shortVoyageRecordService;

public ShortVoyageRecordsController(
    ILoggerFactory loggerFactory,
    IRepository<ShortVoyageRecord> repository,
    IShortVoyageRecordService shortVoyageRecordService) : base(loggerFactory, repository)
{
    _shortVoyageRecordService = shortVoyageRecordService ?? throw new ArgumentNullException(nameof(shortVoyageRecordService));
}

// 8. register in DI container
// services.AddScoped<IShortVoyageRecordService, ShortVoyageRecordService>();
// services.AddScoped<IShortVoyageRecordRepository, ShortVoyageRecordRepository>();