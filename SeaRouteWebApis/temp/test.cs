using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public class WaypointsRepository : IWaypointsRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<WaypointsRepository> _logger;

        public WaypointsRepository(ApplicationDbContext dbContext, ILogger<WaypointsRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> AddUpdateWaypointsAsync(List<RoutePoint> waypoints)
        {
            try
            {
                // Note: Since RoutePoint doesn't have UserId and RecordId directly,
                // you'll need to adjust this logic based on your actual RoutePoint model
                // This assumes RoutePoint has RouteVersionId which links to Record

                var routeVersionIds = waypoints.Select(wp => wp.RouteVersionId).Distinct().ToList();

                var existing = await _dbContext.RoutePoints
                    .Where(rp => routeVersionIds.Contains(rp.RouteVersionId))
                    .ToListAsync();

                if (existing != null && existing.Any())
                {
                    _dbContext.RoutePoints.RemoveRange(existing);
                    await _dbContext.SaveChangesAsync();
                }

                await _dbContext.RoutePoints.AddRangeAsync(waypoints);
                int result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<RoutePoint>> GetWaypointsAsync(string userId, string recordId)
        {
            try
            {
                // Convert string recordId to Guid if needed
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                {
                    return new List<RoutePoint>();
                }

                // Since RoutePoint doesn't have direct userId/recordId, 
                // we need to join through RouteVersion -> Record
                return await (from rp in _dbContext.RoutePoints
                              join rv in _dbContext.RouteVersions on rp.RouteVersionId equals rv.RouteVersionId
                              join r in _dbContext.Records on rv.RecordId equals r.RecordId
                              // Add user filtering logic here based on your user-record relationship
                              where r.RecordId == recordGuid && r.IsActive == true
                              select rp).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}