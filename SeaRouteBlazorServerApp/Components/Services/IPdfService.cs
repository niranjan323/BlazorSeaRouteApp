using SeaRouteModel.Models;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IPdfService
{
    Task<byte[]> GenerateReportPdfAsync(ReportData reportData);
    Task DownloadPdfAsync(ReportData reportData, string fileName);
    Task PrintPdfAsync(ReportData reportData);
}
