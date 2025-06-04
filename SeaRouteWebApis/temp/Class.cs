using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using SeaRouteModel.Models;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class RecordService : IRecordService
    {
        private readonly IRecordRepository _recordRepository;
        private readonly ILogger<RecordService> _logger;

        public RecordService(IRecordRepository recordRepository, ILogger<RecordService> logger)
        {
            _recordRepository = recordRepository ?? throw new ArgumentNullException(nameof(recordRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RecordDto>> GetRecordListAsync(string userId)
        {
            try
            {
                var records = await _recordRepository.GetRecordListAsync(userId);
                if (records == null)
                    return new List<RecordDto>();

                return records.Select(x => new RecordDto()
                {
                    RecordId = x.RecordId.ToString(),
                    RecordName = x.RouteName,
                    UserId = userId,
                    ReductionFactor = x.RecordReductionFactors?.FirstOrDefault()?.ReductionFactor ?? 0,
                    DeparturePort = "", // Will need to be populated from route points or voyage legs
                    ArrivalPort = "", // Will need to be populated from route points or voyage legs
                    RouteDistance = x.RouteDistance ?? 0,
                    SeasonType = "", // Will need to be determined from reduction factors
                    VesselIMO = x.RecordVessels?.FirstOrDefault()?.Vessel?.VesselImo ?? "",
                    VesselName = x.RecordVessels?.FirstOrDefault()?.Vessel?.VesselName ?? "",
                    VoyageDate = x.ShortVoyageRecord?.DepartureTime.ToString("MM/dd/yyyy") ?? string.Empty,
                    CalcType = "", // Need to determine how this maps
                    CreatedDate = x.CreatedDate
                }).OrderByDescending(x => x.CreatedDate).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> AddRecordAsync(AddRecordDto addRecordDto)
        {
            try
            {
                var recordId = Guid.NewGuid();

                // Create the main record
                var record = new Record()
                {
                    RecordId = recordId,
                    RouteName = addRecordDto.RouteName,
                    RouteDistance = addRecordDto.RouteDistance,
                    Submitted = true,
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                // Create route version
                var routeVersion = new RouteVersion()
                {
                    RouteVersionId = Guid.NewGuid(),
                    RecordId = recordId,
                    RecordRouteVersion = 1, // First version
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                // Create route points (waypoints)
                var routePoints = new List<RoutePoint>();
                if (addRecordDto.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.Waypoints.Count; i++)
                    {
                        var wp = addRecordDto.Waypoints[i];
                        routePoints.Add(new RoutePoint()
                        {
                            RoutePointId = Guid.NewGuid(),
                            RouteVersionId = routeVersion.RouteVersionId,
                            RoutePointOrder = i + 1,
                            GeoPointId = Guid.Parse(wp.GeoPointId),
                            SegDistance = (float)wp.SegDistance,
                            CreatedBy = Guid.Parse(addRecordDto.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Create voyage legs
                var voyageLegs = new List<VoyageLeg>();
                if (addRecordDto.VoyageLegs?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.VoyageLegs.Count; i++)
                    {
                        var vl = addRecordDto.VoyageLegs[i];
                        voyageLegs.Add(new VoyageLeg()
                        {
                            VoyageLegId = Guid.NewGuid(),
                            RouteVersionId = routeVersion.RouteVersionId,
                            VoyageLegOrder = i + 1,
                            DeparturePort = string.IsNullOrEmpty(vl.DeparturePort) ? null : Guid.Parse(vl.DeparturePort),
                            ArrivalPort = string.IsNullOrEmpty(vl.ArrivalPort) ? null : Guid.Parse(vl.ArrivalPort),
                            Distance = (float?)vl.Distance,
                            CreatedBy = Guid.Parse(addRecordDto.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Create record reduction factor
                var recordReductionFactor = new RecordReductionFactor()
                {
                    RecordId = recordId,
                    SeasonType = byte.Parse(addRecordDto.SeasonType),
                    ReductionFactor = (float)addRecordDto.ReductionFactor,
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                return await _recordRepository.AddRecordAsync(record, routeVersion, routePoints, voyageLegs, recordReductionFactor);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateRecordAsync(AddRecordDto addRecordDto)
        {
            try
            {
                var recordId = Guid.Parse(addRecordDto.RecordId);

                // Get current max version number
                var currentVersion = await _recordRepository.GetMaxRouteVersionAsync(recordId);
                var newVersionNumber = currentVersion + 1;

                // Create new route version
                var routeVersion = new RouteVersion()
                {
                    RouteVersionId = Guid.NewGuid(),
                    RecordId = recordId,
                    RecordRouteVersion = newVersionNumber,
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                // Create route points (waypoints)
                var routePoints = new List<RoutePoint>();
                if (addRecordDto.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.Waypoints.Count; i++)
                    {
                        var wp = addRecordDto.Waypoints[i];
                        routePoints.Add(new RoutePoint()
                        {
                            RoutePointId = Guid.NewGuid(),
                            RouteVersionId = routeVersion.RouteVersionId,
                            RoutePointOrder = i + 1,
                            GeoPointId = Guid.Parse(wp.GeoPointId),
                            SegDistance = (float)wp.SegDistance,
                            CreatedBy = Guid.Parse(addRecordDto.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Create voyage legs
                var voyageLegs = new List<VoyageLeg>();
                if (addRecordDto.VoyageLegs?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.VoyageLegs.Count; i++)
                    {
                        var vl = addRecordDto.VoyageLegs[i];
                        voyageLegs.Add(new VoyageLeg()
                        {
                            VoyageLegId = Guid.NewGuid(),
                            RouteVersionId = routeVersion.RouteVersionId,
                            VoyageLegOrder = i + 1,
                            DeparturePort = string.IsNullOrEmpty(vl.DeparturePort) ? null : Guid.Parse(vl.DeparturePort),
                            ArrivalPort = string.IsNullOrEmpty(vl.ArrivalPort) ? null : Guid.Parse(vl.ArrivalPort),
                            Distance = (float?)vl.Distance,
                            CreatedBy = Guid.Parse(addRecordDto.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Update the main record
                var record = new Record()
                {
                    RecordId = recordId,
                    RouteName = addRecordDto.RouteName,
                    RouteDistance = addRecordDto.RouteDistance,
                    Submitted = true,
                    ModifiedBy = Guid.Parse(addRecordDto.UserId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true
                };

                // Update record reduction factor
                var recordReductionFactor = new RecordReductionFactor()
                {
                    RecordId = recordId,
                    SeasonType = byte.Parse(addRecordDto.SeasonType),
                    ReductionFactor = (float)addRecordDto.ReductionFactor,
                    ModifiedBy = Guid.Parse(addRecordDto.UserId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true
                };

                return await _recordRepository.UpdateRecordAsync(record, routeVersion, routePoints, voyageLegs, recordReductionFactor);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<RecordDetailsDto>> GetRecordByIdAsync(string[] recordIds)
        {
            try
            {
                var guidRecordIds = recordIds.Select(Guid.Parse).ToArray();
                var records = await _recordRepository.GetRecordByIdAsync(guidRecordIds);
                var routePoints = await _recordRepository.GetWaypointsAsync(guidRecordIds);

                List<RecordDetailsDto> lst = [];

                foreach (var record in records)
                {
                    var recordRoutePoints = routePoints.Where(x => x.RecordId == record.RecordId)
                        .Select(x => new RoutePointInfo()
                        {
                            RecordId = x.RecordId.ToString(),
                            WaypointId = x.RoutePointOrder,
                            GeoPointId = x.GeoPointId.ToString(),
                            Latitude = x.GeoPoint?.Latitude ?? 0, // Assuming GeoPoint has Latitude property
                            Longitude = x.GeoPoint?.Longitude ?? 0, // Assuming GeoPoint has Longitude property
                            SegDistance = x.SegDistance,
                            RoutePointType = "waypoint" // Default value, adjust as needed
                        }).ToList();

                    // Get the latest route version's voyage legs for departure/arrival ports
                    var latestRouteVersion = record.RouteVersions?.OrderByDescending(rv => rv.RecordRouteVersion).FirstOrDefault();
                    var firstVoyageLeg = latestRouteVersion?.VoyageLegs?.OrderBy(vl => vl.VoyageLegOrder).FirstOrDefault();
                    var lastVoyageLeg = latestRouteVersion?.VoyageLegs?.OrderByDescending(vl => vl.VoyageLegOrder).FirstOrDefault();

                    lst.Add(new RecordDetailsDto()
                    {
                        RecordId = record.RecordId.ToString(),
                        RouteName = record.RouteName,
                        ReductionFactor = record.RecordReductionFactors?.FirstOrDefault()?.ReductionFactor ?? 0,
                        DeparturePort = firstVoyageLeg?.DeparturePortNavigation != null ? new PortModel()
                        {
                            Port_Id = firstVoyageLeg.DeparturePortNavigation.GeoPointId.ToString(),
                            Name = "", // Port name property needs to be added to Port model
                            Latitude = 0, // GeoPoint Latitude
                            Longitude = 0, // GeoPoint Longitude
                        } : null,
                        ArrivalPort = lastVoyageLeg?.ArrivalPortNavigation != null ? new PortModel()
                        {
                            Port_Id = lastVoyageLeg.ArrivalPortNavigation.GeoPointId.ToString(),
                            Name = "", // Port name property needs to be added to Port model
                            Latitude = 0, // GeoPoint Latitude
                            Longitude = 0, // GeoPoint Longitude
                        } : null,
                        RoutePoints = recordRoutePoints
                    });
                }

                return lst;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteRecordAsync(string userId, string recordId)
        {
            try
            {
                return await _recordRepository.DeleteRecordAsync(Guid.Parse(userId), Guid.Parse(recordId));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddVoyageLegsAsync(List<VoyageLegDto> voyageLegDtos)
        {
            try
            {
                var voyageLegs = voyageLegDtos.Select(x => new VoyageLeg()
                {
                    VoyageLegId = Guid.NewGuid(),
                    RouteVersionId = Guid.Parse(x.RouteVersionId),
                    VoyageLegOrder = x.VoyageLegOrder,
                    DeparturePort = string.IsNullOrEmpty(x.DeparturePort) ? null : Guid.Parse(x.DeparturePort),
                    ArrivalPort = string.IsNullOrEmpty(x.ArrivalPort) ? null : Guid.Parse(x.ArrivalPort),
                    Distance = (float?)x.Distance,
                    CreatedBy = Guid.Parse(x.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                }).ToList();

                return await _recordRepository.AddVoyageLegsAsync(voyageLegs);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateVoyageLegsAsync(List<VoyageLegDto> voyageLegDtos)
        {
            try
            {
                var voyageLegs = voyageLegDtos.Select(x => new VoyageLeg()
                {
                    VoyageLegId = Guid.Parse(x.VoyageLegId),
                    RouteVersionId = Guid.Parse(x.RouteVersionId),
                    VoyageLegOrder = x.VoyageLegOrder,
                    DeparturePort = string.IsNullOrEmpty(x.DeparturePort) ? null : Guid.Parse(x.DeparturePort),
                    ArrivalPort = string.IsNullOrEmpty(x.ArrivalPort) ? null : Guid.Parse(x.ArrivalPort),
                    Distance = (float?)x.Distance,
                    ModifiedBy = Guid.Parse(x.UserId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true
                }).ToList();

                return await _recordRepository.UpdateVoyageLegsAsync(voyageLegs);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<RouteLegInfo>> GetVoyageLegsAsync(string recordId, string userId)
        {
            try
            {
                var legs = await _recordRepository.GetVoyageLegsAsync(Guid.Parse(recordId), Guid.Parse(userId));

                var routeLegs = legs.Select(le => new RouteLegInfo()
                {
                    RecordId = le.RouteVersion.RecordId.ToString(),
                    RecordLegId = le.VoyageLegOrder,
                    RecordLegName = $"Leg {le.VoyageLegOrder}",
                    ArrivalPort = le.ArrivalPortNavigation?.GeoPointId.ToString() ?? "",
                    ArrivalPortId = le.ArrivalPort?.ToString() ?? "",
                    DeparturePort = le.DeparturePortNavigation?.GeoPointId.ToString() ?? "",
                    DeparturePortId = le.DeparturePort?.ToString() ?? "",
                    Distance = le.Distance ?? 0,
                    ReductionFactor = le.VoyageLegReductionFactors?.FirstOrDefault()?.ReductionFactor ?? 0,
                }).ToList();

                return routeLegs.OrderBy(x => x.RecordLegId).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

