using SeaRouteModel.Models;

namespace SeaRouteWebApis.Interfaces
{
    public interface IPortService
    {
        Task<List<PortModel>> SearchPortsAsync(string searchTerm);
    }
}
