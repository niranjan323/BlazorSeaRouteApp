// Add these variables at the top with other global variables
using System;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

var pendingWaypointSequence = null; // Track which waypoint position is being set
var isWaypointSelectionActive = false;

// Modified click handler - DO NOT add pins directly
map.on('click', function(e) {
    if (isWaypointSelectionActive && pendingWaypointSequence !== null)
    {
        const latitude = e.latlng.lat;
        const longitude = e.latlng.lng;

        // Only capture coordinates - let C# handle the positioning and pin creation
        currentDotNetHelper.invokeMethodAsync('CaptureCoordinatesWithSequence',
            latitude, longitude, pendingWaypointSequence);

        // Reset waypoint selection state
        setWaypointSelection(false);
        pendingWaypointSequence = null;
    }
});

// Modified setWaypointSelection function to accept sequence number
function setWaypointSelection(active, sequenceNumber = null)
{
    isWaypointSelectionActive = active;
    pendingWaypointSequence = sequenceNumber;

    // Update cursor style
    map.getContainer().style.cursor = active ? 'crosshair' : '';

    // Update tooltip display
    if (!active && coordTooltip.style.display !== 'none')
    {
        coordTooltip.style.display = 'none';
    }
}

// New function to add waypoint at specific position (called from C#)
function addWaypointAtPosition(latitude, longitude, name, sequenceNumber)
{
    try
    {
        const lat = parseFloat(latitude);
        const lon = parseFloat(longitude);

        // Create waypoint pin
        const waypointPin = L.marker([lat, lon], {
        shadowPane: false
        }).addTo(map);

        waypointPin.bindPopup(`${ name}: ${ lat.toFixed(5)}, ${ lon.toFixed(5)}`);

// Add to waypoint pins array
waypointPins.push(waypointPin);

// Create route point object
const newRoutePoint = {
            type: 'waypoint',
            latLng: [lat, lon],
            name: name,
            sequenceNumber: sequenceNumber
        };

// Insert at correct position in routePoints array
insertRoutePointAtSequence(newRoutePoint, sequenceNumber);

// Reorganize and zoom
reorganizeRoutePoints();
zoomInThenOut(lat, lon);

return true;
    } catch (error) {
    console.error("Error adding waypoint at position:", error);
    return false;
}
}



===================================================================================================
// Updated waypoint methods with position-aware logic

// Modified method to capture coordinates with sequence information
[JSInvokable]
public void CaptureCoordinatesWithSequence(double latitude, double longitude, int sequenceNumber)
{
    if (reductionFactorCalRef != null)
    {
        // Forward coordinates with sequence to child component
        reductionFactorCalRef.CaptureCoordinatesWithSequence(latitude, longitude, sequenceNumber);
    }
}

// Child component method
public async void CaptureCoordinatesWithSequence(double latitude, double longitude, int sequenceNumber)
{
    try
    {
        // Find the waypoint that should receive these coordinates
        var targetWaypoint = routeModel.DepartureWaypoints
            .FirstOrDefault(wp => string.IsNullOrEmpty(wp.Latitude) && string.IsNullOrEmpty(wp.Longitude));

        if (targetWaypoint != null)
        {
            // Update the waypoint coordinates
            targetWaypoint.Latitude = latitude.ToString();
            targetWaypoint.Longitude = longitude.ToString();

            // Call JavaScript to add the waypoint pin at the correct position
            await JS.InvokeVoidAsync("addWaypointAtPosition", 
                latitude, longitude, $"Waypoint {sequenceNumber}", sequenceNumber);

            StateHasChanged();
            
            // Recalculate route after waypoint is positioned
            await Task.Delay(100); // Small delay to ensure JS completes
            await RecalculateRoute();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in CaptureCoordinatesWithSequence: {ex.Message}");
    }
}

// Updated AddDepartureWaypointAfter method
private async Task AddDepartureWaypointAfter(int afterSequenceNumber)
{
    int insertIndex = routeModel.DepartureItems.FindIndex(x => x.SequenceNumber == afterSequenceNumber) + 1;
    int newSequenceNumber = afterSequenceNumber + 1;

    var waypointModel = new WaypointModel
    {
        SequenceNumber = newSequenceNumber,
        PointId = Guid.NewGuid().ToString()
        // Don't set Latitude/Longitude here - wait for map click
    };

    var newRouteItem = new RouteItemModel
    {
        SequenceNumber = newSequenceNumber,
        ItemType = "W",
        Waypoint = waypointModel
    };

    routeModel.DepartureItems.Insert(insertIndex, newRouteItem);
    routeModel.DepartureWaypoints.Add(waypointModel);
    ResequenceDepartureItems();

    // Enable waypoint selection with sequence number
    await EnableWaypointSelectionWithSequence(newSequenceNumber);
    StateHasChanged();
}

// Updated AddDepartureWaypoint method (for first waypoint)
private async Task AddDepartureWaypoint()
{
    var waypointModel = new WaypointModel
    {
        SequenceNumber = 1,
        PointId = Guid.NewGuid().ToString()
        // Don't set coordinates here - wait for map click
    };
    
    var newRouteItem = new RouteItemModel
    {
        SequenceNumber = 1,
        ItemType = "W",
        Waypoint = waypointModel
    };
    
    routeModel.DepartureItems.Insert(0, newRouteItem);
    routeModel.DepartureWaypoints.Add(waypointModel);
    ResequenceDepartureItems();
    
    await EnableWaypointSelectionWithSequence(1);
    StateHasChanged();
}

// Updated EnableWaypointSelection method
private async Task EnableWaypointSelectionWithSequence(int sequenceNumber)
{
    if (JS is not null)
    {
        await JS.InvokeVoidAsync("setWaypointSelection", true, sequenceNumber);
    }
}

// Keep the old method for backward compatibility
private async Task EnableWaypointSelection()
{
    await EnableWaypointSelectionWithSequence(1);
}

// Updated remove waypoint method to properly clean up
private async Task RemoveDepartureWaypoint(WaypointModel waypoint)
{
    if (JS is not null)
    {
        await JS.InvokeVoidAsync("setWaypointSelection", false);
        if (!string.IsNullOrEmpty(waypoint.Latitude) && !string.IsNullOrEmpty(waypoint.Longitude))
        {
            var lat = double.Parse(waypoint.Latitude);
            var lon = double.Parse(waypoint.Longitude);
            await JS.InvokeVoidAsync("removeWaypoint", lat, lon);
        }
    }

    // Remove from route items
    var itemToRemove = routeModel.DepartureItems.FirstOrDefault(i => i.ItemType == "W" && i.Waypoint == waypoint);
    if (itemToRemove != null)
    {
        var removedSequenceNumber = itemToRemove.SequenceNumber;
        routeModel.DepartureItems.Remove(itemToRemove);
        
        // Update sequence numbers for items after the removed one
        foreach (var item in routeModel.DepartureItems.Where(x => x.SequenceNumber > removedSequenceNumber))
        {
            item.SequenceNumber--;
            if (item.ItemType == "P" && item.Port != null)
            {
                item.Port.SequenceNumber = item.SequenceNumber;
            }
            else if (item.ItemType == "W" && item.Waypoint != null)
            {
                item.Waypoint.SequenceNumber = item.SequenceNumber;
            }
        }
    }

    // Remove from waypoints list
    routeModel.DepartureWaypoints.Remove(waypoint);
    StateHasChanged();
    
    // Recalculate route
    await RecalculateRoute();
}