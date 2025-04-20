using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeaRouteWebApis.Controllers
{
    [Route("api/v1/portsapi")]
    [ApiController]
    public class SeaPorts : SeaRouteBaseController<CPorts>
    {
        private readonly IRepository<CPorts> _portRepository;
        private readonly IPythonApiService _pythonApiService;
        private readonly IPortService _portService;
        private readonly ILogger<SeaPorts> _logger;

        public SeaPorts(ILoggerFactory loggerFactory, IRepository<CPorts> portRepository, IPythonApiService pythonApiService, IPortService portService)
            : base(loggerFactory, portRepository)
        {
            _portRepository = portRepository;
            _pythonApiService = pythonApiService;
            _logger = loggerFactory.CreateLogger<SeaPorts>();
            _portService = portService;
        }
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<PortModel>>> SearchPorts(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term cannot be empty");
                }

                if (searchTerm.Length < 2)
                {
                    return BadRequest("Search term must be at least 2 characters");
                }

                var results = await _portService.SearchPortsAsync(searchTerm);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during port search");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
        }







        //[HttpGet("search")]
        //public async Task<ActionResult<IEnumerable<CPorts>>> SearchPorts(string searchTerm)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        //        {
        //            return Ok(new List<CPorts>());
        //        }

        //        // Use mock data instead of DB for testing
        //        var mockPorts = GetMockPorts()
        //            .Where(p =>
        //                (!string.IsNullOrEmpty(p.PortName) && p.PortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
        //                (!string.IsNullOrEmpty(p.Unlocode) && p.Unlocode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
        //                (!string.IsNullOrEmpty(p.CountryCode) && p.CountryCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
        //            .Take(10)
        //            .ToList();

        //        return Ok(mockPorts);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error searching ports");
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

        // 🔹 Mock data using CPorts
        private List<CPorts> GetMockPorts()
        {
            return new List<CPorts>
            {
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "SGSIN",
                    PortName = "Singapore",
                    CountryCode = "SG",
                    PortAuthority = "Maritime and Port Authority of Singapore",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "NLRTM",
                    PortName = "Rotterdam",
                    CountryCode = "NL",
                    PortAuthority = "Port of Rotterdam Authority",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "CNSHA",
                    PortName = "Shanghai",
                    CountryCode = "CN",
                    PortAuthority = "Shanghai International Port Group",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "USLAX",
                    PortName = "Los Angeles",
                    CountryCode = "US",
                    PortAuthority = "Port of Los Angeles",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "DEHAM",
                    PortName = "Hamburg",
                    CountryCode = "DE",
                    PortAuthority = "Hamburg Port Authority",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "KRPUS",
                    PortName = "Busan",
                    CountryCode = "KR",
                    PortAuthority = "Busan Port Authority",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "FRMRS",
                    PortName = "Marseille",
                    CountryCode = "FR",
                    PortAuthority = "Marseille Fos Port",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "AUSYD",
                    PortName = "Sydney",
                    CountryCode = "AU",
                    PortAuthority = "Port Authority of New South Wales",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "INBOM",
                    PortName = "Mumbai",
                    CountryCode = "IN",
                    PortAuthority = "Mumbai Port Trust",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                },
                new CPorts
                {
                    PointId = Guid.NewGuid(),
                    Unlocode = "AEDXB",
                    PortName = "Dubai",
                    CountryCode = "AE",
                    PortAuthority = "DP World",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "mock",
                    IsActive = true
                }
            };
        }
    }
}
