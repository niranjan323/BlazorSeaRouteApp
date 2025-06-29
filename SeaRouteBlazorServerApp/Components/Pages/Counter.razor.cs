//processAllRouteSegments






try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
    await JS.InvokeVoidAsync("processAllRouteSegments", cts.Token, allRouteJsons.ToArray(), orderedPoints.Count - 1);
}
catch (TaskCanceledException ex)
{
    Console.WriteLine($"JavaScript call timed out or was cancelled: {ex.Message}");
    // Continue with C# processing even if JS fails
}
catch (Exception ex)
{
    Console.WriteLine($"Error calling JavaScript function: {ex.Message}");
    // Continue with C# processing even if JS fails
}