using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;
using SeaRouteModel.Models;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class WaypointsService : IWaypointsService
    {
        private readonly ILogger<WaypointsService> _logger;
        private readonly IWaypointsRepository _waypointsRepository;

        public WaypointsService(ILogger<WaypointsService> logger, IWaypointsRepository waypointsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _waypointsRepository = waypointsRepository ?? throw new ArgumentNullException(nameof(waypointsRepository));
        }

        public async Task<bool> AddUpdateWaypointsAsync(List<WaypointDto> wayPointDtos)
        {
            try
            {
                List<RoutePoint> wayPoints = [];
                wayPointDtos.ForEach(wpDto => wayPoints.Add(new RoutePoint()
                {
                    RouteVersionId = Guid.Parse(wpDto.RouteVersionId), // Changed from UserId/RecordId/WaypointsVersion
                    RoutePointOrder = wpDto.RoutePointOrder,
                    GeoPointId = wpDto.GeoPointId,
                    SegDistance = (float)wpDto.SegDistance, // Cast to float
                    CreatedBy = Guid.Parse(wpDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                }));
                return await _waypointsRepository.AddUpdateWaypointsAsync(wayPoints);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<WaypointDto>> GetWaypointsAsync(string userId, string recordId)
        {
            try
            {
                List<WaypointDto> waypointDtos = [];
                var list = await _waypointsRepository.GetWaypointsAsync(userId, recordId);
                if (list != null)
                {
                    list.ForEach(wp => waypointDtos.Add(new WaypointDto()
                    {
                        RoutePointId = wp.RoutePointId.ToString(),
                        RouteVersionId = wp.RouteVersionId.ToString(),
                        RoutePointOrder = wp.RoutePointOrder,
                        GeoPointId = wp.GeoPointId,
                        SegDistance = wp.SegDistance,
                        UserId = wp.CreatedBy?.ToString() ?? string.Empty // Map from CreatedBy
                    }));
                }
                return waypointDtos;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}