namespace SeaRouteBlazorServerApp.Components.Services;

public class RouteService
{   
    public List<KeyValuePair<string, string>> RouteNames { get; set; } = new();

    public RouteService()
    {
        // Initialize with some sample routes
        AddRouteName("route1", "Marseille-Shanghai");
        AddRouteName("route2", "Rotterdam-Mumbai");
        AddRouteName("route3", "Singapore-Tokyo");
        AddRouteName("route4", "Los Angeles-Sydney");
        AddRouteName("route5", "Hamburg-Dubai");
    }

    public void AddRouteName(string key, string value)
    {
        RouteNames.Add(new KeyValuePair<string, string>(key, value));
    }

    public List<KeyValuePair<string, string>> GetRouteNames()
    {
        return RouteNames;
    }

    public void RemoveRouteName(string key)
    {
        var itemToRemove = RouteNames.FirstOrDefault(x => x.Key == key);
        if (!itemToRemove.Equals(default(KeyValuePair<string, string>)))
        {
            RouteNames.Remove(itemToRemove);
        }
    }

    public void ClearRouteNames()
    {
        RouteNames.Clear();
    }

    public bool HasRoute(string key)
    {
        return RouteNames.Any(x => x.Key == key);
    }
}