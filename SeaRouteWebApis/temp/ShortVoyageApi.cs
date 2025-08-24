// 1. Response Objects in DAL (NextGenEngApps.DigitalRules.CRoute.DAL.Repositories)
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.API.Services;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;
using SeaRouteWebApis.Interfaces;
using SeaRouteWebApis.Services;
using System;
using System.Collections.Generic;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public class ShortVoyageRecordRestoreResponse
    {
        public string RecordId { get; set; }
        public string RouteName { get; set; }
        public double ReductionFactor { get; set; }
        public double RouteDistance { get; set; }
        public List<RestoreRoutePointInfo> RoutePoints { get; set; } = new List<RestoreRoutePointInfo>();
        public RestoreVesselInfo Vessel { get; set; }
        public DateTime? RecordDate { get; set; }
        public ShortVoyageInfo ShortVoyage { get; set; }
    }

    public class RestoreRoutePointInfo
    {
        public string GeoPointId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string RoutePointType { get; set; }
        public int RoutePointOrder { get; set; }
        public RestorePortData PortData { get; set; }
    }

    public class RestorePortData
    {
        public string PortCode { get; set; }
        public string PortName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }

    public class RestoreVesselInfo
    {
        public string VesselName { get; set; }
        public string ImoNumber { get; set; }
        public string Flag { get; set; }
        public double Breadth { get; set; }
        public DateTime? ReportDate { get; set; }
    }

    public class ShortVoyageInfo
    {
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ForecastTime { get; set; }
        public double? ForecastSwellHeight { get; set; }
        public double? ForecastWindHeight { get; set; }
        public double? ReductionFactor { get; set; }
    }
}

// 2. Updated Service Interface
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IShortVoyageRecordService
    {
        Task<ShortVoyageRecordRestoreResponse> RestoreShortVoyageRecordAsync(Guid recordId);
    }
}

// 3. Updated Service Implementation
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class ShortVoyageRecordService : IShortVoyageRecordService
    {
        private readonly IShortVoyageRecordRepository _shortVoyageRecordRepository;
        private readonly IRecordService _recordService; // Reuse existing record service
        private readonly ILogger<ShortVoyageRecordService> _logger;

        public ShortVoyageRecordService(
            IShortVoyageRecordRepository shortVoyageRecordRepository,
            IRecordService recordService,
            ILogger<ShortVoyageRecordService> logger)
        {
            _shortVoyageRecordRepository = shortVoyageRecordRepository ?? throw new ArgumentNullException(nameof(shortVoyageRecordRepository));
            _recordService = recordService ?? throw new ArgumentNullException(nameof(recordService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ShortVoyageRecordRestoreResponse> RestoreShortVoyageRecordAsync(Guid recordId)
        {
            try
            {
                _logger.LogInformation($"Restoring short voyage record with ID: {recordId}");

                // Part 1: Get record and route version related data using existing service
                var editRecordDto = await _recordService.EditRecordAsync(recordId);
                if (editRecordDto == null)
                {
                    _logger.LogWarning($"Record not found for ID: {recordId}");
                    return null;
                }

                // Part 2: Get short voyage specific data
                var shortVoyageData = await _shortVoyageRecordRepository.GetShortVoyageDataAsync(recordId);
                if (shortVoyageData == null)
                {
                    _logger.LogWarning($"Short voyage data not found for record ID: {recordId}");
                    return null;
                }

                // Calculate short voyage reduction factor
                var shortVoyageReductionFactor = CalculateShortVoyageReductionFactor(
                    shortVoyageData.ForecastSwellHeight,
                    shortVoyageData.ForecastWindHeight,
                    editRecordDto.Vessel?.Breadth ?? 0);

                // Map to response object
                var response = new ShortVoyageRecordRestoreResponse
                {
                    RecordId = editRecordDto.RecordId,
                    RouteName = editRecordDto.RouteName,
                    ReductionFactor = editRecordDto.ReductionFactor,
                    RouteDistance = editRecordDto.RouteDistance,
                    RecordDate = editRecordDto.RecordDate,
                    Vessel = new RestoreVesselInfo
                    {
                        VesselName = editRecordDto.Vessel?.VesselName ?? string.Empty,
                        ImoNumber = editRecordDto.Vessel?.IMONumber ?? string.Empty,
                        Flag = editRecordDto.Vessel?.Flag ?? string.Empty,
                        Breadth = editRecordDto.Vessel?.Breadth ?? 0,
                        ReportDate = null // Set based on your business logic
                    },
                    RoutePoints = editRecordDto.RoutePoints?.Select(rp => new RestoreRoutePointInfo
                    {
                        GeoPointId = rp.GeoPointId,
                        Latitude = rp.Latitude,
                        Longitude = rp.Longitude,
                        RoutePointType = rp.RoutePointType,
                        RoutePointOrder = rp.RoutePointOrder,
                        PortData = rp.PortData != null ? new RestorePortData
                        {
                            PortCode = rp.PortData.PortCode,
                            PortName = rp.PortData.PortName,
                            CountryCode = rp.PortData.CountryCode,
                            CountryName = rp.PortData.CountryName
                        } : null
                    }).ToList() ?? new List<RestoreRoutePointInfo>(),
                    ShortVoyage = new ShortVoyageInfo
                    {
                        DepartureTime = shortVoyageData.DepartureTime,
                        ArrivalTime = shortVoyageData.ArrivalTime,
                        ForecastTime = shortVoyageData.ForecastTime,
                        ForecastSwellHeight = shortVoyageData.ForecastSwellHeight,
                        ForecastWindHeight = shortVoyageData.ForecastWindHeight,
                        ReductionFactor = shortVoyageReductionFactor
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring short voyage record with ID: {recordId}");
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

// 4. Updated Repository Interface
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces
{
    public interface IShortVoyageRecordRepository
    {
        Task<ShortVoyageInfo> GetShortVoyageDataAsync(Guid recordId);
    }
}

// 5. Updated Repository Implementation (Only for Part 2 - Short Voyage Data)
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Repositories.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

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

        public async Task<ShortVoyageInfo> GetShortVoyageDataAsync(Guid recordId)
        {
            try
            {
                var shortVoyageData = await (from r in _dbContext.Records
                                             join svr in _dbContext.ShortVoyageRecords on r.RecordId equals svr.RecordId
                                             where r.RecordId == recordId && r.IsActive == true && svr.IsActive == true
                                             select new ShortVoyageInfo
                                             {
                                                 DepartureTime = svr.DepartureTime,
                                                 ArrivalTime = svr.ArrivalTime,
                                                 ForecastTime = svr.ForecastTime,
                                                 ForecastSwellHeight = svr.ForecastHswell,
                                                 ForecastWindHeight = svr.ForecastHwind
                                             }).FirstOrDefaultAsync();

                return shortVoyageData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving short voyage data for record ID: {recordId}");
                throw;
            }
        }
    }
}

// 6. Updated Controller Method
[HttpGet("{record_id}/restore")]
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

// 7. Updated Controller Constructor - Add dependencies
private readonly IShortVoyageRecordService _shortVoyageRecordService;

public ShortVoyageRecordsController(
    ILoggerFactory loggerFactory,
    IRepository<ShortVoyageRecord> repository,
    IShortVoyageRecordService shortVoyageRecordService) : base(loggerFactory, repository)
{
    _shortVoyageRecordService = shortVoyageRecordService ?? throw new ArgumentNullException(nameof(shortVoyageRecordService));
}

// 8. DI Registration
// services.AddScoped<IShortVoyageRecordService, ShortVoyageRecordService>();
// services.AddScoped<IShortVoyageRecordRepository, ShortVoyageRecordRepository>();