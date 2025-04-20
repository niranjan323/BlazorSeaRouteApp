using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;

namespace SeaRouteWebApis.Services
{
    public class PortService : IPortService
    {
        //private readonly IRepository<CPorts> _portRepository;
        //private readonly ILogger<PortService> _logger;
        ////private readonly ApplicationDbContext _context;

        //public PortService(
        //    IRepository<CPorts> portRepository,
        //    ILogger<PortService> logger
        //   // ApplicationDbContext context
        //   )
        //{
        //    _portRepository = portRepository;
        //    _logger = logger;
        //   // _context = context;
        //}

        //public async Task<List<PortModel>> SearchPortsAsync(string searchTerm)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        //        {
        //            return new List<PortModel>();
        //        }

        //        // Using EF Core to perform search with eager loading of related data
        //        var ports = await _context.Ports
        //            .Include(p => p.Country)
        //            .Include(p => p.GeoPoint)
        //            .Where(p =>
        //                (p.PortName != null && p.PortName.Contains(searchTerm)) ||
        //                (p.Unlocode != null && p.Unlocode.Contains(searchTerm)) ||
        //                (p.CountryCode != null && p.CountryCode.Contains(searchTerm)) ||
        //                (p.Country != null && p.Country.CountryName != null && p.Country.CountryName.Contains(searchTerm)))
        //            .Take(10)
        //            .ToListAsync();

        //        // Map to PortModel
        //        var result = ports.Select(MapToPortModel).ToList();

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while searching ports");
        //        throw;
        //    }
        //}

        //private PortModel MapToPortModel(CPorts port)
        //{
        //    return new PortModel
        //    {
        //        Port_Id = port.PointId.ToString(),
        //        Name = port.PortName ?? string.Empty,
        //        Country_Id = port.Country?.CountryId.ToString() ?? string.Empty,
        //        Country = port.Country?.CountryName ?? string.Empty,
        //        Country_Code = port.CountryCode ?? string.Empty,
        //        Unlocode = port.Unlocode ?? string.Empty,
        //        Port_Authority = port.PortAuthority ?? string.Empty,
        //        Latitude = port.GeoPoint?.Latitude ?? 0,
        //        Longitude = port.GeoPoint?.Longitude ?? 0,
        //        Last_Updated = port.ModifiedDate ?? port.CreatedDate
        //        // Other fields would need to be mapped if data is available
        //    };
        //}
        public Task<List<PortModel>> SearchPortsAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }
    }
}
