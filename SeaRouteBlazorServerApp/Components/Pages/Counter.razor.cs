
using Microsoft.JSInterop;
using SeaRouteModel.Models;

private async Task EnableWaypointSelection(int? targetSequenceNumber = null)
{
    if (JS != null)
    {
        // Modified by Niranjan - Pass sequence number to JavaScript
        if (targetSequenceNumber.HasValue)
        {
            await JS.InvokeVoidAsync("setWaypointSelection", true, targetSequenceNumber.Value);
        }
        else
        {
            await JS.InvokeVoidAsync("setWaypointSelection", true);
        }
    }
}

// Modified by Niranjan - Enhanced AddDepartureWaypointAfter to use sequence-aware waypoint selection
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

    routeModel.DepartureItems.Insert(insertIndex, newRouteItem);
    routeModel.DepartureWaypoints.Add(waypointModel);

    ResequenceDepartureItemsEnhanced();

    // Modified by Niranjan - Enable waypoint selection with target sequence number
    await EnableWaypointSelection(afterSequenceNumber + 1);
    StateHasChanged();
}

// Modified by Niranjan - Enhanced parent class CaptureCoordinates method
[JSInvokable]
public void CaptureCoordinates(double latitude, double longitude)
{
    if (reductionFactorCalRef != null)
    {
        // Forward coordinates to child component
        reductionFactorCalRef.CaptureCoordinates(latitude, longitude);
    }
}

// Modified by Niranjan - New method to handle coordinates with sequence
[JSInvokable]
public void CaptureCoordinatesWithSequence(double latitude, double longitude, int sequenceNumber)
{
    if (reductionFactorCalRef != null)
    {
        // Forward coordinates with sequence to child component
        reductionFactorCalRef.CaptureCoordinatesWithSequence(latitude, longitude, sequenceNumber);
    }
}

// Modified by Niranjan - Enhanced child page CaptureCoordinates method
public void CaptureCoordinates(double latitude, double longitude)
{
    if (routeModel.DepartureWaypoints.Count > 0)
    {
        var lastWaypoint = routeModel.DepartureWaypoints.Last();
        lastWaypoint.Latitude = latitude.ToString();
        lastWaypoint.Longitude = longitude.ToString();
        StateHasChanged();
    }
}

// Modified by Niranjan - New method in child page to handle sequence-based coordinate capture
public void CaptureCoordinatesWithSequence(double latitude, double longitude, int sequenceNumber)
{
    // Find the waypoint by sequence number
    var targetWaypoint = routeModel.DepartureWaypoints
        .FirstOrDefault(w => w.SequenceNumber == sequenceNumber);

    if (targetWaypoint != null)
    {
        targetWaypoint.Latitude = latitude.ToString();
        targetWaypoint.Longitude = longitude.ToString();
        StateHasChanged();
    }
    else
    {
        // Fallback to the last waypoint if sequence-based lookup fails
        CaptureCoordinates(latitude, longitude);
    }
}

// Modified by Niranjan - Enhanced CheckAndUpdateMap method for waypoints
private async Task CheckAndUpdateMap(WaypointModel waypoint)
{
    if (double.TryParse(waypoint.Latitude, out double lat) &&
        double.TryParse(waypoint.Longitude, out double lon))
    {
        if (JS != null)
        {
            // Modified by Niranjan - Use editWaypoint with sequence number
            await JS.InvokeVoidAsync("editWaypoint", lat, lon, waypoint.SequenceNumber);
        }
    }
}
==============================================================================================
// Modified by Niranjan - Enhanced editWaypoint function with sequence support
function editWaypoint(lat, lon, sequenceNumber = null) {
    const latitude = lat;
    const longitude = lon;
    
    // Use a single operation for both marker creation and array update
    const newPin = L.marker([latitude, longitude], {
        // Disable shadow for performance
        shadowPane: false
    }).addTo(map);
    
    // Add popup only when clicked, not on creation
    newPin.bindPopup(`Waypoint: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`);
    
    // Store pin references
    clickedPins.push(newPin);
    waypointPins.push(newPin);
    
    // Modified by Niranjan - Create waypoint with proper sequence support
    const newRoutePoint = {
        type: 'waypoint',
        latLng: [latitude, longitude],
        name: `Waypoint ${waypointPins.length}`,
        sequenceNumber: sequenceNumber || (routePoints.length + 1)
    };
    
    // Modified by Niranjan - Insert at correct position if sequence number is provided
    if (sequenceNumber !== null) {
        insertRoutePointAtSequence(newRoutePoint, sequenceNumber);
    } else {
        routePoints.push(newRoutePoint);
    }
    
    // Use optimized zoom function
    zoomInThenOut(latitude, longitude);
    
    // Modified by Niranjan - Capture coordinates with sequence information
    if (sequenceNumber !== null) {
        currentDotNetHelper.invokeMethodAsync('CaptureCoordinatesWithSequence', latitude, longitude, sequenceNumber);
    } else {
        currentDotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);
    }
    
    // Only recalculate route once if we have enough points
    if (canCalculateRoute()) {
        reorganizeRoutePoints();
        // Use requestAnimationFrame for smoother UI updates
        window.requestAnimationFrame(() => {
            currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
        });
    }
    
    // Disable selection after click
    setWaypointSelection(false);
}

// Modified by Niranjan - Enhanced map click handler to support sequence-aware waypoint addition
map.on('click', function (e) {
    if (isWaypointSelectionActive) {
        const latitude = e.latlng.lat;
        const longitude = e.latlng.lng;

        // Modified by Niranjan - Get the target sequence number for the new waypoint
        // This should be set when waypoint selection is activated
        const targetSequenceNumber = window.pendingWaypointSequence || null;

        // Use a single operation for both marker creation and array update
        const newPin = L.marker([latitude, longitude], {
            // Disable shadow for performance
            shadowPane: false
        }).addTo(map);

        // Add popup only when clicked, not on creation
        newPin.bindPopup(`Waypoint: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`);

        // Store pin references
        clickedPins.push(newPin);
        waypointPins.push(newPin);

        // Modified by Niranjan - Create waypoint with sequence support
        const newRoutePoint = {
            type: 'waypoint',
            latLng: [latitude, longitude],
            name: `Waypoint ${waypointPins.length}`,
            sequenceNumber: targetSequenceNumber || (routePoints.length + 1)
        };

        // Modified by Niranjan - Insert at correct position
        if (targetSequenceNumber !== null) {
            insertRoutePointAtSequence(newRoutePoint, targetSequenceNumber);
        } else {
            routePoints.push(newRoutePoint);
        }

        // Use optimized zoom function
        zoomInThenOut(latitude, longitude);

        // Modified by Niranjan - Capture coordinates with sequence
        if (targetSequenceNumber !== null) {
            currentDotNetHelper.invokeMethodAsync('CaptureCoordinatesWithSequence', latitude, longitude, targetSequenceNumber);
        } else {
            currentDotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);
        }

        // Only recalculate route once if we have enough points
        if (canCalculateRoute()) {
            reorganizeRoutePoints();
            // Use requestAnimationFrame for smoother UI updates
            window.requestAnimationFrame(() => {
                currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
            });
        }

        // Modified by Niranjan - Clear pending sequence and disable selection
        window.pendingWaypointSequence = null;
        setWaypointSelection(false);
    }
});

// Modified by Niranjan - Enhanced setWaypointSelection to support sequence
function setWaypointSelection(active, targetSequenceNumber = null) {
    isWaypointSelectionActive = active;
    
    // Modified by Niranjan - Store the target sequence number for waypoint placement
    if (active && targetSequenceNumber !== null) {
        window.pendingWaypointSequence = targetSequenceNumber;
    } else if (!active) {
        window.pendingWaypointSequence = null;
    }

    // Update cursor style once
    map.getContainer().style.cursor = active ? 'crosshair' : '';

    // Only update tooltip display if needed
    if (!active && coordTooltip.style.display !== 'none') {
        coordTooltip.style.display = 'none';
    }
}