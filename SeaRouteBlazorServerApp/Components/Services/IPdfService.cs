using SeaRouteModel.Models;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IPdfService
{
    Task<byte[]> GenerateReportPdfAsync(string html, string? baseUrl = null);
    Task DownloadPdfAsync(string html, string fileName, string? baseUrl = null);
    Task DownloadPdfAsyncAlternative(string html, string fileName, string? baseUrl = null);
}
