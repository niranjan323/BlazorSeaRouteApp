using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Repositories
{
    public class RecordRepository : IRecordRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RecordRepository> _logger;

        public RecordRepository(ApplicationDbContext dbContext, ILogger<RecordRepository> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RecordInfo>> GetRecordListAsync(string userId)
        {
            try
            {
                var routeList = new List<RecordInfo>();

                // Convert userId to appropriate type if needed
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    return routeList;
                }

                routeList = await (from rec in _dbContext.Records
                                   join rf in _dbContext.RecordReductionFactors on rec.RecordId equals rf.RecordId
                                   join ru in _dbContext.RecordUsers on rec.RecordId equals ru.RecordId
                                   join rv in _dbContext.RecordVessels on rec.RecordId equals rv.RecordId into vessels
                                   from ve in vessels.DefaultIfEmpty()
                                   join vessel in _dbContext.Vessels on ve.VesselId equals vessel.VesselId into vesselDetails
                                   from vd in vesselDetails.DefaultIfEmpty()
                                   where rec.IsActive == true && ru.UserId == userGuid
                                   select new RecordInfo()
                                   {
                                       RecordId = rec.RecordId,
                                       RecordName = rec.RouteName,
                                       UserId = ru.UserId,
                                       ReductionFactor = rf.ReductionFactor,
                                       // Note: You'll need to add departure/arrival port logic based on your model
                                       // DeparturePort = dpo.PortName,
                                       // ArrivalPort = apo.PortName,
                                       RouteDistance = rec.RouteDistance ?? 0,
                                       SeasonType = rf.SeasonType.ToString(),
                                       VesselIMO = vd != null ? vd.VesselName : string.Empty, // Adjust based on your Vessel model
                                       VesselName = vd != null ? vd.VesselName : string.Empty,
                                       VoyageDate = rec.ShortVoyageRecord != null ? rec.CreatedDate : null,
                                       CalcType = "Reduction Factor", // Adjust logic as needed
                                       CreatedDate = rec.CreatedDate
                                   }).ToListAsync();

                return routeList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching records: {ex.Message}");
                throw;
            }
        }

        public async Task<string> AddRecordAsync(Record record, List<VoyageLeg> voyageLegs, List<RoutePoint> wayPoints)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.Records.Add(record);

                if (voyageLegs.Count > 0)
                    _dbContext.VoyageLegs.AddRange(voyageLegs);

                if (wayPoints.Count > 0)
                    _dbContext.RoutePoints.AddRange(wayPoints);

                int recCount = await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return record.RecordId.ToString();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateRecordAsync(Record record, List<VoyageLeg> voyageLegs, List<RoutePoint> wayPoints)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var exRecord = await _dbContext.Records.FirstOrDefaultAsync(x => x.RecordId == record.RecordId);

                if (exRecord != null)
                {
                    // Remove existing reduction factors
                    var exRFR = await _dbContext.RecordReductionFactors
                        .Where(rf => rf.RecordId == record.RecordId)
                        .ToListAsync();
                    if (exRFR.Count > 0)
                        _dbContext.RecordReductionFactors.RemoveRange(exRFR);

                    // Remove existing voyage legs
                    var exLegs = await _dbContext.VoyageLegs
                        .Where(vl => vl.RouteVersion.RecordId == record.RecordId)
                        .ToListAsync();
                    if (exLegs.Count > 0)
                        _dbContext.VoyageLegs.RemoveRange(exLegs);

                    // Remove existing route points (waypoints)
                    var exWayPoints = await _dbContext.RoutePoints
                        .Where(rp => rp.RouteVersion.RecordId == record.RecordId)
                        .ToListAsync();
                    if (exWayPoints.Count > 0)
                        _dbContext.RoutePoints.RemoveRange(exWayPoints);

                    // Update record properties
                    exRecord.RouteName = record.RouteName;
                    exRecord.RouteDistance = record.RouteDistance;
                    exRecord.Submitted = true;
                    exRecord.ModifiedBy = record.ModifiedBy;
                    exRecord.ModifiedDate = DateTime.Now;

                    _dbContext.Records.Update(exRecord);

                    if (voyageLegs.Count > 0)
                        _dbContext.VoyageLegs.AddRange(voyageLegs);

                    if (wayPoints.Count > 0)
                        _dbContext.RoutePoints.AddRange(wayPoints);
                }

                int recCount = await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return recCount > 0;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Record>> GetRecordByIdAsync(string[] recordIds)
        {
            try
            {
                // Convert string array to Guid array
                var guidRecordIds = recordIds.Select(id => Guid.Parse(id)).ToArray();

                List<Record> records = await _dbContext.Records
                    .Where(x => guidRecordIds.Contains(x.RecordId))
                    .Include(x => x.RouteVersions)
                        .ThenInclude(rv => rv.VoyageLegs)
                            .ThenInclude(vl => vl.DeparturePortNavigation)
                                .ThenInclude(p => p.GeoPoint)
                    .Include(x => x.RouteVersions)
                        .ThenInclude(rv => rv.VoyageLegs)
                            .ThenInclude(vl => vl.ArrivalPortNavigation)
                                .ThenInclude(p => p.GeoPoint)
                    .ToListAsync();

                return records;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteRecordAsync(string userId, string recordId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return false;

                Record? record = await _dbContext.Records.FirstOrDefaultAsync(x => x.RecordId == recordGuid);

                if (record != null)
                {
                    record.IsActive = false;

                    // Inactive reduction factor records
                    var reductionFactorRecords = await _dbContext.RecordReductionFactors
                        .Where(x => x.RecordId == recordGuid)
                        .ToListAsync();
                    _dbContext.RecordReductionFactors.RemoveRange(reductionFactorRecords);

                    // Inactive voyage legs
                    var voyageLegs = await _dbContext.VoyageLegs
                        .Where(x => x.RouteVersion.RecordId == recordGuid)
                        .ToListAsync();
                    if (voyageLegs.Count > 0)
                        _dbContext.VoyageLegs.RemoveRange(voyageLegs);

                    // Inactive route points (waypoints)
                    var wayPoints = await _dbContext.RoutePoints
                        .Where(x => x.RouteVersion.RecordId == recordGuid)
                        .ToListAsync();
                    if (wayPoints.Count > 0)
                        _dbContext.RoutePoints.RemoveRange(wayPoints);

                    // Inactive vessels relationships
                    var recordVessels = await _dbContext.RecordVessels
                        .Where(x => x.RecordId == recordGuid)
                        .ToListAsync();
                    if (recordVessels.Count > 0)
                        _dbContext.RecordVessels.RemoveRange(recordVessels);

                    int result = await _dbContext.SaveChangesAsync();
                    if (result > 0)
                        await transaction.CommitAsync();

                    return result > 0;
                }

                return false;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public int GetRecordIdAsync()
        {
            try
            {
                return _dbContext.Records.Any() ?
                    _dbContext.Records.Count() + 1 : 1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public int GetWaypointVersionAsync(string userId, string recordId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return 1;

                var routeVersions = _dbContext.RouteVersions
                    .Where(rv => rv.RecordId == recordGuid)
                    .Select(rv => rv.RecordRouteVersion)
                    .ToList();

                return routeVersions.Any() ? routeVersions.Max() + 1 : 1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddVoyageLegsAsync(List<VoyageLeg> voyageLegs)
        {
            try
            {
                await _dbContext.VoyageLegs.AddRangeAsync(voyageLegs);
                int result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateVoyageLegsAsync(List<VoyageLeg> voyageLegs)
        {
            try
            {
                var routeVersionIds = voyageLegs.Select(vl => vl.RouteVersionId).Distinct().ToList();

                var existing = await _dbContext.VoyageLegs
                    .Where(vl => routeVersionIds.Contains(vl.RouteVersionId))
                    .ToListAsync();

                if (existing.Any())
                    _dbContext.VoyageLegs.RemoveRange(existing);

                await _dbContext.VoyageLegs.AddRangeAsync(voyageLegs);
                int result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<RecordLegInfo>> GetVoyageLegsAsync(string recordId, string userId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return new List<RecordLegInfo>();

                var legs = await (from rec in _dbContext.Records
                                  join rv in _dbContext.RouteVersions on rec.RecordId equals rv.RecordId
                                  join leg in _dbContext.VoyageLegs on rv.RouteVersionId equals leg.RouteVersionId
                                  join apo in _dbContext.Ports on leg.ArrivalPort equals apo.GeoPointId
                                  join dpo in _dbContext.Ports on leg.DeparturePort equals dpo.GeoPointId
                                  join recVessel in _dbContext.RecordVessels on rec.RecordId equals recVessel.RecordId into vessels
                                  from rv_vessel in vessels.DefaultIfEmpty()
                                  join vessel in _dbContext.Vessels on rv_vessel.VesselId equals vessel.VesselId into vesselDetails
                                  from vd in vesselDetails.DefaultIfEmpty()
                                  where rec.IsActive == true && rec.RecordId == recordGuid
                                  select new RecordLegInfo()
                                  {
                                      RecordId = rec.RecordId.ToString(),
                                      RecordLegId = leg.VoyageLegId.ToString(),
                                      RecordLegName = string.Empty,
                                      ReductionFactor = 0, // You'll need to get this from VoyageLegReductionFactors
                                      DeparturePort = dpo.PortName,
                                      DeparturePortId = dpo.GeoPointId.ToString(),
                                      ArrivalPort = apo.PortName,
                                      ArrivalPortId = apo.GeoPointId.ToString(),
                                      RouteDistance = leg.Distance ?? 0,
                                      VesselIMO = vd != null ? vd.VesselName : string.Empty, // Adjust based on your Vessel model
                                      VesselName = vd != null ? vd.VesselName : string.Empty,
                                  }).ToListAsync();

                return legs;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<RoutePointInfo>> GetWaypointsAsync(string[] recordIds)
        {
            try
            {
                var guidRecordIds = recordIds.Select(id => Guid.Parse(id)).ToArray();

                var waypoints = await (from rec in _dbContext.Records
                                       join rv in _dbContext.RouteVersions on rec.RecordId equals rv.RecordId
                                       join rp in _dbContext.RoutePoints on rv.RouteVersionId equals rp.RouteVersionId
                                       join gp in _dbContext.GeoPoints on rp.GeoPointId equals gp.GeoPointId
                                       join po in _dbContext.Ports on rp.GeoPointId equals po.GeoPointId into ports
                                       from port in ports.DefaultIfEmpty()
                                       where guidRecordIds.Contains(rec.RecordId)
                                       select new RoutePointInfo
                                       {
                                           RecordId = rec.RecordId.ToString(),
                                           WaypointId = rp.RoutePointId.ToString(),
                                           GeoPointId = rp.GeoPointId.ToString(),
                                           Latitude = gp.Latitude,
                                           Longitude = gp.Longitude,
                                           SegDistance = rp.SegDistance,
                                           RoutePointType = port != null ? "P" : "W"
                                       }).Distinct().OrderBy(x => x.WaypointId).ToListAsync();

                return waypoints;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Vessel>> GetVesselsByRecordIdAsync(string recordId)
        {
            try
            {
                if (!Guid.TryParse(recordId, out Guid recordGuid))
                    return new List<Vessel>();

                return await (from rv in _dbContext.RecordVessels
                              join v in _dbContext.Vessels on rv.VesselId equals v.VesselId
                              join r in _dbContext.Records on rv.RecordId equals r.RecordId
                              where r.IsActive == true && rv.RecordId == recordGuid
                              select v).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}