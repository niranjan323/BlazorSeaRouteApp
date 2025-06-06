using SeaRouteBlazorServerApp.Components;
using SeaRouteBlazorServerApp.Components.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Register a SINGLE default HttpClient with BaseAddress
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7081/")
});

builder.Services.AddScoped<RouteService>();
builder.Services.AddScoped<DebounceService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB limit
    options.StreamBufferCapacity = 15;
    options.EnableDetailedErrors = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
