using SeaRouteModel.Models;

public async Task<List<RecordDto>> GetRecordListAsync(string userId)
{
    try
    {
        // todo for non abs user
        bool isAbsUser = true;
        _ = Guid.TryParse(userId, out Guid userGuid);
        var user = isAbsUser
            ? await _userRepository.GetUserByAbsIdAsync(userGuid)
            : await _userRepository.GetByIdAsync(userGuid);

        Guid actualUserId = user.UserId;
        var records = await _recordRepository.GetRecordListAsync(actualUserId.ToString());
        if (records == null)
            return [];

        return records.Select(x => new RecordDto()
        {
            RecordId = x.RecordId,
            RecordName = x.RecordName ?? string.Empty,
            UserId = userId,
            RouteDistance = x.RouteDistance,
            ReductionFactors = new ReductionFactors
            {
                Annual = x.AnnualReductionFactor,
                Spring = x.SpringReductionFactor,
                Summer = x.SummerReductionFactor,
                Fall = x.FallReductionFactor,
                Winter = x.WinterReductionFactor
            },
            DeparturePort = x.DeparturePort,
            ArrivalPort = x.ArrivalPort,
            // Add UNLOCODE fields
            DeparturePortUNLOCODE = x.DeparturePortUNLOCODE,
            ArrivalPortUNLOCODE = x.ArrivalPortUNLOCODE,
            VesselIMO = x.Vessel.IMONumber,
            VesselName = x.Vessel.VesselName,
            CalcType = x.CalcType,
            CreatedDate = x.CreatedDate
        }).ToList();
    }
    catch (Exception)
    {
        throw;
    }
}

// Updated Repository Method
public async Task<List<RecordInfo>> GetRecordListAsync(string userId)
{
    try
    {
        var routeList = new List<RecordInfo>();

        if (!Guid.TryParse(userId, out Guid userGuid))
            return routeList;

        var routeListQuery = (from rec in _dbContext.Records
                              join ru in _dbContext.RecordUsers.Where(r => r.IsActive == true) on rec.RecordId equals ru.RecordId
                              join rv in _dbContext.RecordVessels.Where(v => v.IsActive == true) on rec.RecordId equals rv.RecordId into vessels
                              from ve in vessels.DefaultIfEmpty()
                              join vessel in _dbContext.Vessels on ve.VesselId equals vessel.VesselId into vesselDetails
                              from vd in vesselDetails.DefaultIfEmpty()
                              where rec.IsActive == true && ru.UserId == userGuid
                              select new RecordInfo()
                              {
                                  RecordId = rec.RecordId.ToString(),
                                  RecordName = rec.RouteName,
                                  UserId = ru.UserId.ToString(),
                                  AnnualReductionFactor = rec.RecordReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Annual)!.ReductionFactor,
                                  SpringReductionFactor = rec.RecordReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Spring)!.ReductionFactor,
                                  SummerReductionFactor = rec.RecordReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Summer)!.ReductionFactor,
                                  FallReductionFactor = rec.RecordReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Fall)!.ReductionFactor,
                                  WinterReductionFactor = rec.RecordReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Winter)!.ReductionFactor,
                                  RouteDistance = rec.RouteDistance ?? 0,
                                  Vessel = new VesselInfo()
                                  {
                                      IMONumber = vd != null ? vd.VesselImo : string.Empty,
                                      VesselName = vd != null ? vd.VesselName : string.Empty,
                                      Breadth = default,
                                      Flag = vd != null ? vd.Flag : string.Empty
                                  },
                                  VoyageDate = rec.ShortVoyageRecord != null ? rec.CreatedDate : null,
                                  CalcType = rec.ShortVoyageRecord != null ? "Short Voyage Reduction Factor" : "Reduction Factor",
                                  CreatedDate = rec.CreatedDate,
                              }).AsQueryable();

        // Updated query to include UNLOCODE
        var query = (from rv in _dbContext.RouteVersions
                     join rec in _dbContext.Records on rv.RecordId equals rec.RecordId
                     join ru in _dbContext.RecordUsers.Where(r => r.IsActive == true) on rec.RecordId equals ru.RecordId
                     join vl in _dbContext.RoutePoints on rv.RouteVersionId equals vl.RouteVersionId
                     where rv.IsActive == true && vl.IsActive == true && rec.IsActive == true && ru.UserId == userGuid
                     group vl by new { rv.RecordId, rv.RouteVersionId } into g
                     select new RecordInfo
                     {
                         RecordId = g.Key.RecordId.ToString(),
                         DeparturePort = g.OrderBy(v => v.RoutePointOrder)
                             .Select(v => v.GeoPoint.Port.PortName).FirstOrDefault(),
                         ArrivalPort = g.OrderByDescending(v => v.RoutePointOrder)
                             .Select(v => v.GeoPoint.Port.PortName).FirstOrDefault(),
                         // Add UNLOCODE fields
                         DeparturePortUNLOCODE = g.OrderBy(v => v.RoutePointOrder)
                             .Select(v => v.GeoPoint.Port.UNLOCODE).FirstOrDefault(),
                         ArrivalPortUNLOCODE = g.OrderByDescending(v => v.RoutePointOrder)
                             .Select(v => v.GeoPoint.Port.UNLOCODE).FirstOrDefault()
                     }).AsQueryable();

        var records = await (from rl in routeListQuery
                             join p in query on rl.RecordId equals p.RecordId
                             select new RecordInfo()
                             {
                                 RecordId = rl.RecordId,
                                 RecordName = rl.RecordName,
                                 UserId = rl.UserId,
                                 AnnualReductionFactor = rl.AnnualReductionFactor,
                                 SpringReductionFactor = rl.SpringReductionFactor,
                                 SummerReductionFactor = rl.SummerReductionFactor,
                                 FallReductionFactor = rl.FallReductionFactor,
                                 WinterReductionFactor = rl.WinterReductionFactor,
                                 DeparturePort = p.DeparturePort,
                                 ArrivalPort = p.ArrivalPort,
                                 // Include UNLOCODE in final result
                                 DeparturePortUNLOCODE = p.DeparturePortUNLOCODE,
                                 ArrivalPortUNLOCODE = p.ArrivalPortUNLOCODE,
                                 RouteDistance = rl.RouteDistance,
                                 Vessel = rl.Vessel,
                                 VoyageDate = rl.VoyageDate,
                                 CalcType = rl.CalcType,
                                 CreatedDate = rl.CreatedDate,
                             }).ToListAsync();
        return records;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"An error occurred while fetching records: {ex.Message}");
        throw;
    }
}


// Updated RecordDto
public class RecordDto
{
    public string RecordId { get; set; }
    public string RecordName { get; set; }
    public string UserId { get; set; }
    public double RouteDistance { get; set; }
    public ReductionFactors ReductionFactors { get; set; }
    public string DeparturePort { get; set; }
    public string ArrivalPort { get; set; }
    // Add UNLOCODE properties
    public string DeparturePortUNLOCODE { get; set; }
    public string ArrivalPortUNLOCODE { get; set; }
    public string VesselIMO { get; set; }
    public string VesselName { get; set; }
    public string CalcType { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Updated RecordInfo
public class RecordInfo
{
    public string RecordId { get; set; }
    public string RecordName { get; set; }
    public string UserId { get; set; }
    public double AnnualReductionFactor { get; set; }
    public double SpringReductionFactor { get; set; }
    public double SummerReductionFactor { get; set; }
    public double FallReductionFactor { get; set; }
    public double WinterReductionFactor { get; set; }
    public string DeparturePort { get; set; }
    public string ArrivalPort { get; set; }
    // Add UNLOCODE properties
    public string DeparturePortUNLOCODE { get; set; }
    public string ArrivalPortUNLOCODE { get; set; }
    public double RouteDistance { get; set; }
    public VesselInfo Vessel { get; set; }
    public DateTime? VoyageDate { get; set; }
    public string CalcType { get; set; }
    public DateTime CreatedDate { get; set; }
}