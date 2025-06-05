using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using NextGenEngApps.DigitalRules.CRoute.API.Dtos;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Context;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;
using SeaRouteModel.Models;
using System.Data.Entity.ModelConfiguration.Conventions;

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
                    RecordName = x.RouteName ?? string.Empty,
                    UserId = userId, // Pass from parameter since not in model
                    RouteDistance = x.RouteDistance ?? 0,
                    // Note: These properties need to be retrieved from related entities or calculated
                    ReductionFactor = 0, // Will need to get from RecordReductionFactors
                    DeparturePort = string.Empty, // Will need to get from navigation
                    ArrivalPort = string.Empty, // Will need to get from navigation
                    SeasonType = string.Empty, // Will need to get from RecordReductionFactors
                    VesselIMO = string.Empty, // Will need to get from RecordVessels
                    VesselName = string.Empty, // Will need to get from RecordVessels
                    VoyageDate = string.Empty, // Will need to get from ShortVoyageRecord
                    CalcType = string.Empty, // Add this property to model if needed
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
                Guid recordId = Guid.NewGuid();
                Guid routeVersionId = Guid.NewGuid();

                // Create RouteVersion
                RouteVersion routeVersion = new RouteVersion()
                {
                    RouteVersionId = routeVersionId,
                    RecordId = recordId,
                    RecordRouteVersion = 1, // Start with version 1
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                };

                // Create VoyageLegs
                List<VoyageLeg> voyageLegs = [];
                if (addRecordDto.VoyageLegs?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.VoyageLegs.Count; i++)
                    {
                        var vl = addRecordDto.VoyageLegs[i];
                        voyageLegs.Add(new VoyageLeg()
                        {
                            VoyageLegId = Guid.NewGuid(),
                            RouteVersionId = routeVersionId,
                            VoyageLegOrder = i + 1,
                            DeparturePort = vl.DeparturePort,
                            ArrivalPort = vl.ArrivalPort,
                            Distance = (float?)vl.Distance,
                            CreatedBy = Guid.Parse(vl.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true,
                        });
                    }
                }

                // Create RoutePoints
                List<RoutePoint> routePoints = [];
                if (addRecordDto.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.Waypoints.Count; i++)
                    {
                        var wp = addRecordDto.Waypoints[i];
                        routePoints.Add(new RoutePoint()
                        {
                            RoutePointId = Guid.NewGuid(),
                            RouteVersionId = routeVersionId,
                            RoutePointOrder = i + 1,
                            GeoPointId = wp.GeoPointId,
                            SegDistance = (float)wp.SegDistance,
                            CreatedBy = Guid.Parse(wp.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Create Record
                Record record = new Record()
                {
                    RecordId = recordId,
                    RouteName = addRecordDto.RouteName,
                    RouteDistance = addRecordDto.RouteDistance,
                    Submitted = true,
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                };

                // Create RecordReductionFactor if SeasonType is provided
                List<RecordReductionFactor> reductionFactors = [];
                if (!string.IsNullOrEmpty(addRecordDto.SeasonType) && byte.TryParse(addRecordDto.SeasonType, out byte seasonType))
                {
                    reductionFactors.Add(new RecordReductionFactor()
                    {
                        RecordId = recordId,
                        SeasonType = seasonType,
                        ReductionFactor = (float)addRecordDto.ReductionFactor,
                        CreatedBy = Guid.Parse(addRecordDto.UserId),
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    });
                }

                var result = await _recordRepository.AddRecordAsync(record, routeVersion, voyageLegs, routePoints, reductionFactors);
                return result ? recordId.ToString() : string.Empty;
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
                Guid recordId = Guid.Parse(addRecordDto.RecordId);

                // Get next version number
                int nextVersion = await _recordRepository.GetNextRouteVersionAsync(recordId);
                Guid routeVersionId = Guid.NewGuid();

                // Create new RouteVersion
                RouteVersion routeVersion = new RouteVersion()
                {
                    RouteVersionId = routeVersionId,
                    RecordId = recordId,
                    RecordRouteVersion = nextVersion,
                    CreatedBy = Guid.Parse(addRecordDto.UserId),
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                };

                // Create VoyageLegs
                List<VoyageLeg> voyageLegs = [];
                if (addRecordDto.VoyageLegs?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.VoyageLegs.Count; i++)
                    {
                        var vl = addRecordDto.VoyageLegs[i];
                        voyageLegs.Add(new VoyageLeg()
                        {
                            VoyageLegId = Guid.NewGuid(),
                            RouteVersionId = routeVersionId,
                            VoyageLegOrder = i + 1,
                            DeparturePort = vl.DeparturePort,
                            ArrivalPort = vl.ArrivalPort,
                            Distance = (float?)vl.Distance,
                            CreatedBy = Guid.Parse(vl.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true,
                        });
                    }
                }

                // Create RoutePoints
                List<RoutePoint> routePoints = [];
                if (addRecordDto.Waypoints?.Count > 0)
                {
                    for (int i = 0; i < addRecordDto.Waypoints.Count; i++)
                    {
                        var wp = addRecordDto.Waypoints[i];
                        routePoints.Add(new RoutePoint()
                        {
                            RoutePointId = Guid.NewGuid(),
                            RouteVersionId = routeVersionId,
                            RoutePointOrder = i + 1,
                            GeoPointId = wp.GeoPointId,
                            SegDistance = (float)wp.SegDistance,
                            CreatedBy = Guid.Parse(wp.UserId),
                            CreatedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }

                // Update Record
                Record record = new Record()
                {
                    RecordId = recordId,
                    RouteName = addRecordDto.RouteName,
                    RouteDistance = addRecordDto.RouteDistance,
                    Submitted = true,
                    ModifiedBy = Guid.Parse(addRecordDto.UserId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true,
                };

                return await _recordRepository.UpdateRecordAsync(record, routeVersion, voyageLegs, routePoints);
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
                var records = await _recordRepository.GetRecordByIdAsync(recordIds);
                var routePoints = await _recordRepository.GetWaypointsAsync(recordIds);
                List<RecordDetailsDto> lst = [];

                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    var recordRoutePoints = routePoints.Where(x => x.RouteVersionId.ToString() == record.RecordId.ToString())
                        .Select(x => new RoutePointInfo()
                        {
                            RecordId = x.RouteVersionId.ToString(),
                            WaypointId = x.RoutePointOrder,
                            GeoPointId = x.GeoPointId.ToString(),
                            Latitude = 0, // Get from GeoPoint navigation
                            Longitude = 0, // Get from GeoPoint navigation
                            SegDistance = x.SegDistance,
                            RoutePointType = "waypoint" // Default value
                        }).ToList();

                    lst.Add(new RecordDetailsDto()
                    {
                        RecordId = record.RecordId.ToString(),
                        RouteName = record.RouteName ?? string.Empty,
                        ReductionFactor = 0, // Get from RecordReductionFactors
                        DeparturePort = new PortModel()
                        {
                            Port_Id = string.Empty, // Get from first VoyageLeg
                            Name = string.Empty,
                            Latitude = 0,
                            Longitude = 0,
                        },
                        ArrivalPort = new PortModel()
                        {
                            Port_Id = string.Empty, // Get from last VoyageLeg
                            Name = string.Empty,
                            Latitude = 0,
                            Longitude = 0,
                        },
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
                return await _recordRepository.DeleteRecordAsync(userId, recordId);
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
                List<VoyageLeg> voyageLegs = [];
                for (int i = 0; i < voyageLegDtos.Count; i++)
                {
                    var x = voyageLegDtos[i];
                    voyageLegs.Add(new VoyageLeg()
                    {
                        VoyageLegId = Guid.NewGuid(),
                        RouteVersionId = Guid.Parse(x.RouteVersionId),
                        VoyageLegOrder = i + 1,
                        DeparturePort = x.DeparturePort,
                        ArrivalPort = x.ArrivalPort,
                        Distance = (float?)x.Distance,
                        CreatedBy = Guid.Parse(x.UserId),
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    });
                }

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
                List<VoyageLeg> voyageLegs = [];
                for (int i = 0; i < voyageLegDtos.Count; i++)
                {
                    var x = voyageLegDtos[i];
                    voyageLegs.Add(new VoyageLeg()
                    {
                        VoyageLegId = Guid.NewGuid(),
                        RouteVersionId = Guid.Parse(x.RouteVersionId),
                        VoyageLegOrder = i + 1,
                        DeparturePort = x.DeparturePort,
                        ArrivalPort = x.ArrivalPort,
                        Distance = (float?)x.Distance,
                        CreatedBy = Guid.Parse(x.UserId),
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    });
                }

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
                List<RouteLegInfo> routeLegs = [];
                var legs = await _recordRepository.GetVoyageLegsAsync(recordId, userId);

                foreach (var le in legs)
                {
                    routeLegs.Add(new RouteLegInfo()
                    {
                        RecordId = recordId,
                        VoyageLegId = le.VoyageLegOrder,
                        RecordLegId = le.VoyageLegOrder,
                        RecordLegName = $"Leg {le.VoyageLegOrder}",
                        ArrivalPort = string.Empty, // Get from Port navigation
                        ArrivalPortId = le.ArrivalPort?.ToString() ?? string.Empty,
                        DeparturePort = string.Empty, // Get from Port navigation
                        DeparturePortId = le.DeparturePort?.ToString() ?? string.Empty,
                        Distance = le.Distance ?? 0,
                        ReductionFactor = 0, // Get from VoyageLegReductionFactors
                    });
                }
                return routeLegs.OrderBy(x => x.RecordLegId).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<VesselDto>> GetVesselsByRecordIdAsync(string recordId)
        {
            try
            {
                List<Vessel> vesselList = await _recordRepository.GetVesselsByRecordIdAsync(recordId);
                List<VesselDto> vessels = new List<VesselDto>();
                vesselList.ForEach(v =>
                {
                    vessels.Add(new VesselDto()
                    {
                        VesselIMO = v.VesselImo,
                        VesselName = v.VesselName ?? string.Empty
                    });
                });
                return vessels;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}