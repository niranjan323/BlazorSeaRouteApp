using Microsoft.JSInterop;
using SeaRouteModel.Models;
using System.Text;
using PuppeteerSharp;

namespace SeaRouteBlazorServerApp.Components.Services;

public class PdfService : IPdfService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IJSRuntime jsRuntime, ILogger<PdfService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<byte[]> GenerateReportPdfAsync(string html, string? baseUrl = null)
    {
        try
        {
            // Download browser if not already downloaded
            await new BrowserFetcher().DownloadAsync();
            
            var launchOptions = new LaunchOptions 
            { 
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            };
            
            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();
            
            if (!string.IsNullOrEmpty(baseUrl))
            {
                await page.SetContentAsync(html, new NavigationOptions 
                { 
                    WaitUntil = new[] { WaitUntilNavigation.Load }, 
                    Timeout = 60000 
                });
            }
            else
            {
                await page.SetContentAsync(html);
            }
            
            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PuppeteerSharp.Media.PaperFormat.A4,
                MarginOptions = new PuppeteerSharp.Media.MarginOptions 
                { 
                    Top = "20px", 
                    Bottom = "20px", 
                    Left = "15px", 
                    Right = "15px" 
                },
                PrintBackground = true
            });
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF with PuppeteerSharp");
            throw;
        }
    }

    public async Task DownloadPdfAsync(string html, string fileName, string? baseUrl = null)
    {
        try
        {
            var pdfBytes = await GenerateReportPdfAsync(html, baseUrl);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogError("PDF bytes are null or empty");
                throw new InvalidOperationException("Generated PDF is empty");
            }
            
            var base64String = Convert.ToBase64String(pdfBytes);
            _logger.LogInformation($"PDF generated successfully. Size: {pdfBytes.Length} bytes, Base64 length: {base64String.Length}");
            
            // Check if the JavaScript function exists
            var functionExists = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.downloadFileFromBase64 === 'function'");
            if (!functionExists)
            {
                _logger.LogError("downloadFileFromBase64 JavaScript function not found");
                throw new InvalidOperationException("JavaScript download function not available");
            }
            
            // Call the JavaScript function with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", cts.Token, fileName, base64String, "application/pdf");
            
            _logger.LogInformation("PDF download initiated successfully");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "JavaScript call timed out");
            throw new InvalidOperationException("PDF download timed out. Please try again.", ex);
        }
        catch (JSException jsEx)
        {
            _logger.LogError(jsEx, "JavaScript error during PDF download");
            throw new InvalidOperationException($"JavaScript error: {jsEx.Message}", jsEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF report");
            throw;
        }
    }

    // Alternative method using a simpler JavaScript approach
    public async Task DownloadPdfAsyncAlternative(string html, string fileName, string? baseUrl = null)
    {
        try
        {
            var pdfBytes = await GenerateReportPdfAsync(html, baseUrl);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogError("PDF bytes are null or empty");
                throw new InvalidOperationException("Generated PDF is empty");
            }
            
            // Use a simpler JavaScript approach
            await _jsRuntime.InvokeVoidAsync("downloadPdf", fileName, Convert.ToBase64String(pdfBytes));
            
            _logger.LogInformation("PDF download initiated successfully using alternative method");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF report using alternative method");
            throw;
        }
    }
}