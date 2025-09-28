using SeaRouteModel.Models;

private WaypointModel currentWaypointBeingPlaced = null;
private int currentWaypointInsertPosition = -1;



private async Task AddDepartureWaypointAfter(int afterSequenceNumber)
{
    int insertIndex = routeModel.DepartureItems.FindIndex(x => x.SequenceNumber == afterSequenceNumber) + 1;

    var waypointModel = new WaypointModel
    {
        SequenceNumber = afterSequenceNumber + 1,
        PointId = Guid.NewGuid().ToString()
    };

    var newRouteItem = new RouteItemModel
    {
        SequenceNumber = afterSequenceNumber + 1,
        ItemType = "W",
        Waypoint = waypointModel
    };

    // Store the waypoint that's currently being placed
    currentWaypointBeingPlaced = waypointModel;
    currentWaypointInsertPosition = insertIndex;


    await EnableWaypointSelection();
    StateHasChanged();
}

// Updated CaptureCoordinates method
public void CaptureCoordinates(double latitude, double longitude)
{
    if (currentWaypointBeingPlaced != null)
    {
        // Set coordinates for the waypoint being placed
        currentWaypointBeingPlaced.Latitude = latitude.ToString();
        currentWaypointBeingPlaced.Longitude = longitude.ToString();

        // Create the route item
        var newRouteItem = new RouteItemModel
        {
            SequenceNumber = currentWaypointBeingPlaced.SequenceNumber,
            ItemType = "W",
            Waypoint = currentWaypointBeingPlaced
        };

        // Insert at the correct position
        if (currentWaypointInsertPosition >= 0 && currentWaypointInsertPosition <= routeModel.DepartureItems.Count)
        {
            routeModel.DepartureItems.Insert(currentWaypointInsertPosition, newRouteItem);
        }
        else
        {
            routeModel.DepartureItems.Add(newRouteItem);
        }

        routeModel.DepartureWaypoints.Add(currentWaypointBeingPlaced);

        // Resequence all items
        ResequenceDepartureItems();

        // Clear the current waypoint being placed
        currentWaypointBeingPlaced = null;
        currentWaypointInsertPosition = -1;

        StateHasChanged();
    }
    else if (routeModel.DepartureWaypoints.Count > 0)
    {
        // Fallback to old behavior for regular waypoint addition
        var lastWaypoint = routeModel.DepartureWaypoints.Last();
        lastWaypoint.Latitude = latitude.ToString();
        lastWaypoint.Longitude = longitude.ToString();
        StateHasChanged();
    }
}