using SeaRouteModel.Models;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IPdfService
{
    Task<byte[]> GenerateReportPdfAsync(ReportData reportData);
    Task DownloadPdfAsync(ReportData reportData, string fileName);
    Task<byte[]> GenerateReportPdfAsync(UnifiedReportData reportData);
    Task DownloadPdfAsync(UnifiedReportData reportData, string fileName);

}
