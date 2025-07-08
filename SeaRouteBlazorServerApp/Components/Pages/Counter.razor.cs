using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

public async Task<List<LegModel>> GetRouteLegsList(string recordId)
{
    List<LegModel> result = [];
    try
    {
        string userId = "1";
        var response = await _httpClient.GetAsync($"{Endpoints.ROUTE_LEGS_LIST}?userId={userId}&recordId={recordId}");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<List<LegModel>>();
    }
    catch (Exception ex)
    {
        throw;
    }

    return result;
}

[HttpGet("legs")]
public async Task<IActionResult> GetRecordLegs(string userId, string recordId)
{
    try
    {
        var legs = await _recordService.GetVoyageLegsAsync(recordId, userId);
        legs = legs.Where(x => x.RecordId == recordId).ToList();
        return Ok(legs);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "error occured on GetVoyageLegs");
        return StatusCode((int)StatusCodes.Status500InternalServerError);
    }
}

public async Task<List<RecordLegInfo>> GetVoyageLegsAsync(string recordId, string userId)
{
    try
    {
        if (!Guid.TryParse(recordId, out Guid recordGuid))
            return new List<RecordLegInfo>();
        var legCount = await (from rec in _dbContext.Records
                              join rv in _dbContext.RouteVersions on rec.RecordId equals rv.RecordId
                              join leg in _dbContext.VoyageLegs on rv.RouteVersionId equals leg.RouteVersionId
                              where rec.IsActive == true && rec.RecordId == recordGuid
                              select leg).CountAsync();
        if (legCount <= 1)
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
                          where rec.IsActive == true && rec.RecordId == recordGuid && rv.IsActive == true
                          select new RecordLegInfo()
                          {
                              RecordId = rec.RecordId.ToString(),
                              RecordLegId = leg.VoyageLegId.ToString(),
                              RecordLegName = string.Empty,
                              AnnualReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Annual)!.ReductionFactor ?? default,
                              SpringReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Spring)!.ReductionFactor ?? default,
                              SummerReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Summer)!.ReductionFactor ?? default,
                              FallReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Fall)!.ReductionFactor ?? default,
                              WinterReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Winter)!.ReductionFactor ?? default,
                              DeparturePort = dpo.PortName,
                              DeparturePortId = dpo.GeoPointId.ToString(),
                              ArrivalPort = apo.PortName,
                              ArrivalPortId = apo.GeoPointId.ToString(),
                              RouteDistance = leg.Distance ?? 0,
                              VesselIMO = vd != null ? vd.VesselImo : string.Empty, // Adjust based on your Vessel model
                              VesselName = vd != null ? vd.VesselName : string.Empty,
                              LegOrder = leg.VoyageLegOrder
                          }).OrderBy(x => x.LegOrder).ToListAsync();

        return legs;
    }
    catch (Exception)
    {
        throw;
    }
}


#####################################################################################################################################################################

public async Task<List<LegModel>> GetRouteLegsList(string recordId)
{
    List<LegModel> result = [];
    try
    {
        // Get actual logged-in user ID instead of hardcoded "1"
        string userId = GetCurrentUserId(); // Implement this method to get current user
        var response = await _httpClient.GetAsync($"{Endpoints.ROUTE_LEGS_LIST}?userId={userId}&recordId={recordId}");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<List<LegModel>>();
    }
    catch (Exception ex)
    {
        throw;
    }

    return result;
}

// 2. Fix the controller - remove redundant filtering
[HttpGet("legs")]
public async Task<IActionResult> GetRecordLegs(string userId, string recordId)
{
    try
    {
        var legs = await _recordService.GetVoyageLegsAsync(recordId, userId);
        // Remove this line as filtering is now done in service:
        // legs = legs.Where(x => x.RecordId == recordId).ToList();
        return Ok(legs);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "error occured on GetVoyageLegs");
        return StatusCode((int)StatusCodes.Status500InternalServerError);
    }
}

// 3. Fix the service method to filter by user
public async Task<List<RecordLegInfo>> GetVoyageLegsAsync(string recordId, string userId)
{
    try
    {
        if (!Guid.TryParse(recordId, out Guid recordGuid))
            return new List<RecordLegInfo>();

        if (!Guid.TryParse(userId, out Guid userGuid))
            return new List<RecordLegInfo>();

        var legCount = await (from rec in _dbContext.Records
                              join rv in _dbContext.RouteVersions on rec.RecordId equals rv.RecordId
                              join leg in _dbContext.VoyageLegs on rv.RouteVersionId equals leg.RouteVersionId
                              join ru in _dbContext.RecordUsers on rec.RecordId equals ru.RecordId // Add user mapping
                              where rec.IsActive == true && rec.RecordId == recordGuid && ru.UserId == userGuid
                              select leg).CountAsync();

        if (legCount <= 1)
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
                          join ru in _dbContext.RecordUsers on rec.RecordId equals ru.RecordId // Add user mapping join
                          where rec.IsActive == true && rec.RecordId == recordGuid && rv.IsActive == true && ru.UserId == userGuid // Filter by user
                          select new RecordLegInfo()
                          {
                              RecordId = rec.RecordId.ToString(),
                              RecordLegId = leg.VoyageLegId.ToString(),
                              RecordLegName = string.Empty,
                              AnnualReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Annual)!.ReductionFactor ?? default,
                              SpringReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Spring)!.ReductionFactor ?? default,
                              SummerReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Summer)!.ReductionFactor ?? default,
                              FallReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Fall)!.ReductionFactor ?? default,
                              WinterReductionFactor = leg.VoyageLegReductionFactors.FirstOrDefault(an => an.SeasonType == (byte)SeasonType.Winter)!.ReductionFactor ?? default,
                              DeparturePort = dpo.PortName,
                              DeparturePortId = dpo.GeoPointId.ToString(),
                              ArrivalPort = apo.PortName,
                              ArrivalPortId = apo.GeoPointId.ToString(),
                              RouteDistance = leg.Distance ?? 0,
                              VesselIMO = vd != null ? vd.VesselImo : string.Empty,
                              VesselName = vd != null ? vd.VesselName : string.Empty,
                              LegOrder = leg.VoyageLegOrder
                          }).OrderBy(x => x.LegOrder).ToListAsync();

        return legs;
    }
    catch (Exception)
    {
        throw;
    }
}

private string GetCurrentUserId()
{

    // return HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // return _userService.GetCurrentUserId();
    // return _authService.GetLoggedInUserId();

    throw new NotImplementedException("Implement based on your authentication system");
}