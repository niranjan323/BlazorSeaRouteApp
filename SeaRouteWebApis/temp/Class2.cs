// 1. Add to RecordsController.cs

using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

[HttpGet("record_reduction_factors/{id}")]
public async Task<IActionResult> GetRecordReductionFactors(string id)
{
    try
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest("Record ID is required");

        var result = await _recordService.GetRecordReductionFactorsAsync(id);

        if (result == null)
            return NotFound($"Record with ID {id} not found");

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred on GetRecordReductionFactors");
        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}

[HttpGet("{id}")]
public async Task<IActionResult> GetRecordDetails(string id)
{
    try
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest("Record ID is required");

        var result = await _recordService.GetRecordDetailsAsync(id);

        if (result == null)
            return NotFound($"Record with ID {id} not found");

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred on GetRecordDetails");
        return StatusCode(StatusCodes.Status500InternalServerError);
    }
}

// 2. Add to IRecordService interface

Task<RecordReductionFactorsDto?> GetRecordReductionFactorsAsync(string recordId);
Task<RecordDetailsDto?> GetRecordDetailsAsync(string recordId);

// 3. Add DTOs (create new files or add to existing DTO file)

public class RecordReductionFactorsDto
{
    public string RecordId { get; set; } = string.Empty;
    public ReductionFactorsResponse ReductionFactors { get; set; } = new();
}

public class ReductionFactorsResponse
{
    public double Annual { get; set; }
    public double Spring { get; set; }
    public double Summer { get; set; }
    public double Fall { get; set; }
    public double Winter { get; set; }
}

public class RecordDetailsDto
{
    public string RecordId { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public double RouteDistance { get; set; }
}

// 4. Add to RecordService.cs

public async Task<RecordReductionFactorsDto?> GetRecordReductionFactorsAsync(string recordId)
{
    try
    {
        // Handle case sensitivity by converting to uppercase and then parsing
        recordId = recordId.ToUpper();
        if (!Guid.TryParse(recordId, out Guid recordGuid))
            return null;

        var reductionFactors = await _recordRepository.GetRecordReductionFactorsAsync(recordGuid);

        if (reductionFactors == null)
            return null;

        return new RecordReductionFactorsDto
        {
            RecordId = recordId,
            ReductionFactors = new ReductionFactorsResponse
            {
                Annual = reductionFactors.Annual,
                Spring = reductionFactors.Spring,
                Summer = reductionFactors.Summer,
                Fall = reductionFactors.Fall,
                Winter = reductionFactors.Winter
            }
        };
    }
    catch (Exception)
    {
        throw;
    }
}

public async Task<RecordDetailsDto?> GetRecordDetailsAsync(string recordId)
{
    try
    {
        // Handle case sensitivity by converting to uppercase and then parsing
        recordId = recordId.ToUpper();
        if (!Guid.TryParse(recordId, out Guid recordGuid))
            return null;

        var record = await _recordRepository.GetRecordDetailsAsync(recordGuid);

        if (record == null)
            return null;

        return new RecordDetailsDto
        {
            RecordId = recordId,
            RouteName = record.RouteName ?? string.Empty,
            RouteDistance = record.RouteDistance ?? 0
        };
    }
    catch (Exception)
    {
        throw;
    }
}

// 5. Add to IRecordRepository interface

Task<ReductionFactorsInfo?> GetRecordReductionFactorsAsync(Guid recordId);
Task<RecordBasicInfo?> GetRecordDetailsAsync(Guid recordId);

// 6. Add models for repository response (add to your Models folder)

public class ReductionFactorsInfo
{
    public double Annual { get; set; }
    public double Spring { get; set; }
    public double Summer { get; set; }
    public double Fall { get; set; }
    public double Winter { get; set; }
}

public class RecordBasicInfo
{
    public string? RouteName { get; set; }
    public double? RouteDistance { get; set; }
}

// 7. Add to RecordRepository.cs

public async Task<ReductionFactorsInfo?> GetRecordReductionFactorsAsync(Guid recordId)
{
    try
    {
        var reductionFactors = await _dbContext.RecordReductionFactors
            .Where(rrf => rrf.RecordId == recordId && rrf.IsActive == true)
            .Select(rrf => new { rrf.SeasonType, rrf.ReductionFactor })
            .ToListAsync();

        if (!reductionFactors.Any())
            return null;

        var result = new ReductionFactorsInfo();

        foreach (var factor in reductionFactors)
        {
            switch (factor.SeasonType)
            {
                case (byte)SeasonType.Annual:
                    result.Annual = factor.ReductionFactor;
                    break;
                case (byte)SeasonType.Spring:
                    result.Spring = factor.ReductionFactor;
                    break;
                case (byte)SeasonType.Summer:
                    result.Summer = factor.ReductionFactor;
                    break;
                case (byte)SeasonType.Fall:
                    result.Fall = factor.ReductionFactor;
                    break;
                case (byte)SeasonType.Winter:
                    result.Winter = factor.ReductionFactor;
                    break;
            }
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"An error occurred while fetching reduction factors for record {recordId}: {ex.Message}");
        throw;
    }
}

public async Task<RecordBasicInfo?> GetRecordDetailsAsync(Guid recordId)
{
    try
    {
        var record = await _dbContext.Records
            .Where(r => r.RecordId == recordId && r.IsActive == true)
            .Select(r => new RecordBasicInfo
            {
                RouteName = r.RouteName,
                RouteDistance = r.RouteDistance
            })
            .FirstOrDefaultAsync();

        return record;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"An error occurred while fetching record details for record {recordId}: {ex.Message}");
        throw;
    }
}