var map;
var departurePin;
var arrivalPin;
var routeLayer;
var isWaypointSelectionActive = false;
var coordTooltip;
var currentDotNetHelper;

// Arrays to store different types of pins and route points
let clickedPins = [];
let portPins = [];
let waypointPins = [];
let routePoints = [];

// Store route segments from FastAPI calls
let routeSegments = [];

function initializeMap(dotNetHelper) {
    currentDotNetHelper = dotNetHelper;
    map = L.map('map').setView([20, 60], 3);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors', crossOrigin: 'anonymous'
    }).addTo(map);

    // Initialize the route layer
    routeLayer = L.layerGroup().addTo(map);

    // Create coordinate tooltip
    coordTooltip = L.DomUtil.create('div', 'coord-tooltip');
    coordTooltip.style.position = 'absolute';
    coordTooltip.style.pointerEvents = 'none';
    coordTooltip.style.backgroundColor = 'rgba(255, 255, 255, 0.9)';
    coordTooltip.style.padding = '5px 10px';
    coordTooltip.style.borderRadius = '3px';
    coordTooltip.style.zIndex = '1000';
    coordTooltip.style.display = 'none';
    coordTooltip.style.fontFamily = 'Arial, sans-serif';
    coordTooltip.style.fontSize = '12px';
    coordTooltip.style.border = '1px solid #ccc';
    map.getContainer().appendChild(coordTooltip);

    // Mouse move handler
    map.on('mousemove', function (e) {
        if (isWaypointSelectionActive) {
            const { lat, lng } = e.latlng;
            coordTooltip.textContent = `Click to set waypoint\nLat: ${lat.toFixed(4)}, Lng: ${lng.toFixed(4)}`;
            coordTooltip.style.display = 'block';
            coordTooltip.style.left = (e.originalEvent.clientX + 15) + 'px';
            coordTooltip.style.top = (e.originalEvent.clientY + 15) + 'px';
        }
    });

    map.getContainer().addEventListener('mouseleave', function () {
        coordTooltip.style.display = 'none';
    });

    map.on('click', function (e) {
        if (isWaypointSelectionActive) {
            var latitude = e.latlng.lat;
            var longitude = e.latlng.lng;

            // Call Blazor method
            currentDotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);

            // Add new pin at the clicked location and store it in the array
            let newPin = L.marker([latitude, longitude]).addTo(map);
            newPin.bindPopup(`Waypoint: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`).openPopup();
            clickedPins.push(newPin);

            // Also add it to waypoint pins for clarity
            waypointPins.push(newPin);

            // Add to route points as a waypoint
            routePoints.push({
                type: 'waypoint',
                latLng: [latitude, longitude],
                name: `Waypoint ${waypointPins.length}`
            });

            // Use the zoom in-out effect
            zoomInThenOut(latitude, longitude);

            // Trigger route recalculation if we have departure and arrival
            if (departurePin && arrivalPin) {
                reorganizeRoutePoints();
                // Notify Blazor that route points have changed
                currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
            }

            // Optionally disable selection after click
            setWaypointSelection(false);
        }
    });
}

function setWaypointSelection(active) {
    isWaypointSelectionActive = active;
    if (!active) {
        coordTooltip.style.display = 'none';
        map.getContainer().style.cursor = '';
    }
    else {
        // Change cursor to crosshair when in selection mode
        map.getContainer().style.cursor = 'crosshair';
    }
}

// Function to search for locations and add pins
async function searchLocation(query, isDeparture) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();
        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);

            // Add pin based on type (departure or arrival)
            if (isDeparture) {
                // Remove previous departure pin if exists
                if (departurePin) {
                    map.removeLayer(departurePin);
                }

                // Create new departure pin with default marker
                departurePin = L.marker([lat, lon]).addTo(map);
                departurePin.bindPopup("Departure: " + query).openPopup();

                // Add to route points as departure
                const departureIndex = routePoints.findIndex(p => p.type === 'departure');
                if (departureIndex !== -1) {
                    routePoints[departureIndex] = {
                        type: 'departure',
                        latLng: [lat, lon],
                        name: query
                    };
                } else {
                    routePoints.push({
                        type: 'departure',
                        latLng: [lat, lon],
                        name: query
                    });
                }

                // Zoom in and out animation for departure point
                zoomInThenOut(lat, lon);
            } else {
                // Remove previous arrival pin if exists
                if (arrivalPin) {
                    map.removeLayer(arrivalPin);
                }

                // Create new arrival pin with default marker
                arrivalPin = L.marker([lat, lon]).addTo(map);
                arrivalPin.bindPopup("Arrival: " + query).openPopup();

                // Add to route points as arrival
                const arrivalIndex = routePoints.findIndex(p => p.type === 'arrival');
                if (arrivalIndex !== -1) {
                    routePoints[arrivalIndex] = {
                        type: 'arrival',
                        latLng: [lat, lon],
                        name: query
                    };
                } else {
                    routePoints.push({
                        type: 'arrival',
                        latLng: [lat, lon],
                        name: query
                    });
                }

                // Zoom in and out animation for arrival point
                zoomInThenOut(lat, lon);
            }

            // Reorganize and notify Blazor if we have both departure and arrival points
            if (departurePin && arrivalPin) {
                reorganizeRoutePoints();
                currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
            }
        } else {
            alert("Port not found!");
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Function to add a port based on API search results
async function zoomAndPinLocation(query, isDeparture, lat = null, lon = null) {
    try {
        if (lat === null || lon === null) {
            let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
            let data = await response.json();
            if (data.length > 0) {
                lat = parseFloat(data[0].lat);
                lon = parseFloat(data[0].lon);
            } else {
                alert("Port not found!");
                return;
            }
        }

        // Add pin and store it in the array
        let newPin = L.marker([lat, lon]).addTo(map);
        newPin.bindPopup(`Intermediate Port: ${query}`).openPopup();
        portPins.push(newPin);

        // Add to route points as an intermediate port
        routePoints.push({
            type: 'port',
            latLng: [lat, lon],
            name: query
        });

        reorganizeRoutePoints();
        zoomInThenOut(lat, lon);

        if (departurePin && arrivalPin) {
            currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Ensure route points are properly organized
function reorganizeRoutePoints() {
    // Extract departure and arrival points
    const departurePoint = routePoints.find(p => p.type === 'departure');
    const arrivalPoint = routePoints.find(p => p.type === 'arrival');
    const otherPoints = routePoints.filter(p => p.type !== 'departure' && p.type !== 'arrival');

    // Reset route points array
    routePoints = [];

    // Add departure first
    if (departurePoint) {
        routePoints.push(departurePoint);
    }

    // Add all intermediate points
    routePoints = routePoints.concat(otherPoints);

    // Add arrival last
    if (arrivalPoint) {
        routePoints.push(arrivalPoint);
    }

    // Return the ordered points for use in FastAPI calls
    return routePoints;
}

// Function to create a segment route from FastAPI response
function createSeaRoutefromAPI(routeJson) {
    try {
        // Parse the GeoJSON string if it's not already an object
        const routeData = typeof routeJson === 'string' ? JSON.parse(routeJson) : routeJson;

        // Extract LineString coordinates from the GeoJSON
        const geoJsonCoordinates = routeData.geometry.coordinates;

        // Convert from [longitude, latitude] to [latitude, longitude] for Leaflet
        const leafletCoordinates = geoJsonCoordinates.map(coord => [coord[1], coord[0]]);

        // Return the converted coordinates
        return {
            coordinates: leafletCoordinates,
            properties: routeData.properties
        };
    } catch (error) {
        console.error("Error creating sea route segment:", error);
        return null;
    }
}
// niranjan
// Modify the processRouteSegment function to handle segment info
function processRouteSegmentWithInfo(routeJson, segmentIndex, totalSegments, startName, endName, distance, duration) {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Add segment information
        if (segment) {
            segment.segmentInfo = {
                index: segmentIndex,
                startName: startName,
                endName: endName,
                distance: distance,
                duration: duration
            };
        }

        // Store this segment
        routeSegments[segmentIndex] = segment;

        // If all segments are received, draw the combined route
        if (routeSegments.filter(s => s !== null).length === totalSegments) {
            drawCombinedRouteWithSegmentInfo(routeSegments);
        }

        return segment;
    } catch (error) {
        console.error("Error processing route segment:", error);
        return null;
    }
}

// Updated function to draw route with segment info - simplified style
function drawCombinedRouteWithSegmentInfo(segments) {
    // Clear previous route
    routeLayer.clearLayers();

    if (!segments || segments.length === 0) {
        return;
    }

    // Display each segment with consistent styling
    segments.forEach((segment, index) => {
        if (!segment || !segment.coordinates || segment.coordinates.length < 2) return;

        // Create polyline for this segment - using same color for all segments
        const segmentPolyline = L.polyline(segment.coordinates, {
            color: '#0066ff', // Same blue color for all segments
            weight: 4,
            opacity: 0.8,
            smoothFactor: 1
        }).addTo(routeLayer);

        // Add compact segment info marker at midpoint
        const midIndex = Math.floor(segment.coordinates.length / 2);
        const midPoint = segment.coordinates[midIndex];

        // Format distance and duration
        const formattedDistance = Math.round(segment.segmentInfo.distance * 10) / 10;
        const formattedDuration = Math.round(segment.segmentInfo.duration * 10) / 10;

        // Create compact segment marker
        L.marker(midPoint, {
            icon: L.divIcon({
                className: 'segment-marker',
                html: `<div style="background-color: white; padding: 2px 5px; border-radius: 3px; border: 1px solid #0066ff; font-size: 11px;">
                        ${formattedDistance} km | ${formattedDuration} hrs
                       </div>`,
                iconSize: null
            })
        }).addTo(routeLayer);
    });

    // Add total distance and duration marker with same compact style
    if (segments.length > 0) {
        // Calculate totals
        let totalDistance = 0;
        let totalDuration = 0;
        segments.forEach(segment => {
            if (segment && segment.segmentInfo) {
                totalDistance += segment.segmentInfo.distance;
                totalDuration += segment.segmentInfo.duration;
            }
        });

        // Format totals
        totalDistance = Math.round(totalDistance * 10) / 10;
        totalDuration = Math.round(totalDuration * 10) / 10;

        // Get last segment for the overall route endpoint
        const lastSegment = segments[segments.length - 1];

        // Add total info marker at the route end with compact style
        if (lastSegment && lastSegment.coordinates && lastSegment.coordinates.length > 0) {
            const endPoint = lastSegment.coordinates[lastSegment.coordinates.length - 1];

            L.marker(endPoint, {
                icon: L.divIcon({
                    className: 'total-marker',
                    html: `<div style="background-color: white; padding: 2px 5px; border-radius: 3px; border: 1px solid #ff0000; font-size: 11px;">
                            Total: ${totalDistance} km | ${totalDuration} hrs
                           </div>`,
                    iconSize: null,
                    iconAnchor: [50, 0]
                })
            }).addTo(routeLayer);
        }
    }

    // Calculate bounds to fit the entire route
    let allCoordinates = [];
    segments.forEach(segment => {
        if (segment && segment.coordinates) {
            allCoordinates = allCoordinates.concat(segment.coordinates);
        }
    });

    const routeBounds = L.latLngBounds(allCoordinates);

    // Calculate pixel padding - approximately 40% of map width to the left for your overlay
    const paddingLeft = window.innerWidth * 0.45;

    // Fit the map to the route bounds with appropriate padding
    map.flyToBounds(routeBounds, {
        paddingTopLeft: [paddingLeft, 20],
        paddingBottomRight: [20, 20],
        duration: 1.5
    });
}

// Simplified getSegmentColor function that always returns the same color
function getSegmentColor(index) {
    return '#0066ff'; // Consistent blue color for all segments
}

// Add function to remove a specific route segment
function removeRouteSegment(segmentIndex) {
    // Remove the segment from the array
    if (segmentIndex >= 0 && segmentIndex < routeSegments.length) {
        routeSegments.splice(segmentIndex, 1);

        // Re-index remaining segments
        routeSegments.forEach((segment, index) => {
            if (segment && segment.segmentInfo) {
                segment.segmentInfo.index = index;
            }
        });

        // Redraw the route if we still have segments
        if (routeSegments.length > 0) {
            drawCombinedRouteWithSegmentInfo(routeSegments);
        } else {
            routeLayer.clearLayers();
        }

        return true;
    }
    return false;
}



// Draw combined route from multiple segments
//function drawCombinedRoute(segments) {
//    // Clear previous route
//    routeLayer.clearLayers();

//    if (!segments || segments.length === 0) {
//        return;
//    }

//    // Combine all coordinates from all segments
//    let allCoordinates = [];
//    let totalDistance = 0;
//    let totalDuration = 0;

//    segments.forEach(segment => {
//        if (segment && segment.coordinates) {
//            allCoordinates = allCoordinates.concat(segment.coordinates);

//            // Sum up properties if available
//            if (segment.properties) {
//                if (segment.properties.length) {
//                    totalDistance += Math.round(segment.properties.length);
//                }
//                if (segment.properties.duration_hours) {
//                    totalDuration += Math.round(segment.properties.duration_hours);
//                }
//            }
//        }
//    });

//    // Create the polyline with the combined coordinates
//    const routePolyline = L.polyline(allCoordinates, {
//        color: '#0066ff',
//        weight: 3,
//        opacity: 0.8,
//        smoothFactor: 1,
//        dashArray: null
//    }).addTo(routeLayer);

//    // Add total distance and duration marker at the middle of the route
//    if (allCoordinates.length > 0) {
//        const midIndex = Math.floor(allCoordinates.length / 2);
//        const midPoint = allCoordinates[midIndex];

//        L.marker(midPoint, {
//            icon: L.divIcon({
//                className: 'distance-marker',
//                html: `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
//                        Distance: ${totalDistance} km<br>
//                        Duration: ${totalDuration} hours
//                       </div>`,
//                iconSize: null
//            })
//        }).addTo(routeLayer);
//    }

//    // Calculate bounds to fit the entire route
//    const routeBounds = L.latLngBounds(allCoordinates);

//    // Calculate pixel padding - approximately 40% of map width to the left for your overlay
//    const paddingLeft = window.innerWidth * 0.45;

//    // Fit the map to the route bounds with appropriate padding
//    map.flyToBounds(routeBounds, {
//        paddingTopLeft: [paddingLeft, 20],
//        paddingBottomRight: [20, 20],
//        duration: 1.5
//    });

//    return routePolyline;
//}



// Process a single route segment from FastAPI
//function processRouteSegment(routeJson, segmentIndex, totalSegments) {
//    try {
//        const segment = createSeaRoutefromAPI(routeJson);

//        // Store this segment
//        routeSegments[segmentIndex] = segment;

//        // If all segments are received, draw the combined route
//        if (routeSegments.filter(s => s !== null).length === totalSegments) {
//            drawCombinedRoute(routeSegments);
//        }

//        return segment;
//    } catch (error) {
//        console.error("Error processing route segment:", error);
//        return null;
//    }
//}

// Add marker for port data
function updateMapWithPortData(portData, isDeparture) {
    if (!portData || !portData.latitude || !portData.longitude) return;

    let lat = parseFloat(portData.latitude);
    let lon = parseFloat(portData.longitude);
    let name = portData.name || "Port";
    let unlocode = portData.unlocode || "";

    if (isNaN(lat) || isNaN(lon)) return;

    // Create the marker
    const marker = L.marker([lat, lon]).addTo(map);
    marker.bindPopup(`${name} (${unlocode})<br>Lat: ${lat.toFixed(4)}, Lng: ${lon.toFixed(4)}`).openPopup();

    // Add to appropriate array
    if (isDeparture) {
        if (departurePin) {
            map.removeLayer(departurePin);
        }
        departurePin = marker;

        // Update route points
        const departureIndex = routePoints.findIndex(p => p.type === 'departure');
        if (departureIndex !== -1) {
            routePoints[departureIndex] = {
                type: 'departure',
                latLng: [lat, lon],
                name: name
            };
        } else {
            routePoints.push({
                type: 'departure',
                latLng: [lat, lon],
                name: name
            });
        }
    } else {
        portPins.push(marker);

        // Add to route points as an intermediate port
        routePoints.push({
            type: 'port',
            latLng: [lat, lon],
            name: name
        });
    }

    // Use the new zoom pattern
    zoomInThenOut(lat, lon);

    // Trigger route recalculation if we have departure and arrival
    if (departurePin && arrivalPin) {
        reorganizeRoutePoints();
        currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
    }
}

// Function to add arrival port data
function addArrivalPort(portData) {
    if (!portData || !portData.latitude || !portData.longitude) return;

    let lat = parseFloat(portData.latitude);
    let lon = parseFloat(portData.longitude);
    let name = portData.name || "Arrival Port";
    let unlocode = portData.unlocode || "";

    if (isNaN(lat) || isNaN(lon)) return;

    // Create the marker
    const marker = L.marker([lat, lon]).addTo(map);
    marker.bindPopup(`Arrival: ${name} (${unlocode})<br>Lat: ${lat.toFixed(4)}, Lng: ${lon.toFixed(4)}`).openPopup();

    // Set as arrival pin
    if (arrivalPin) {
        map.removeLayer(arrivalPin);
    }
    arrivalPin = marker;

    // Update route points
    const arrivalIndex = routePoints.findIndex(p => p.type === 'arrival');
    if (arrivalIndex !== -1) {
        routePoints[arrivalIndex] = {
            type: 'arrival',
            latLng: [lat, lon],
            name: name
        };
    } else {
        routePoints.push({
            type: 'arrival',
            latLng: [lat, lon],
            name: name
        });
    }

    // Use the new zoom pattern
    zoomInThenOut(lat, lon);

    // Trigger route recalculation if we have departure point
    if (departurePin) {
        reorganizeRoutePoints();
        currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
    }
}

// Function to initialize a multi-segment route calculation
function initializeRouteCalculation() {
    // Clear previous route segments
    routeSegments = [];

    // Clear previous route layer
    routeLayer.clearLayers();

    // Return the currently organized route points
    return reorganizeRoutePoints();
}

// Reset all map data
function resetMap() {
    // Clear departure and arrival pins
    if (departurePin) {
        map.removeLayer(departurePin);
        departurePin = null;
    }
    if (arrivalPin) {
        map.removeLayer(arrivalPin);
        arrivalPin = null;
    }

    // Clear route layer
    if (routeLayer) {
        routeLayer.clearLayers();
    }

    // Clear all clicked pins
    clickedPins.forEach(pin => {
        map.removeLayer(pin);
    });
    clickedPins = [];

    // Clear all port pins
    portPins.forEach(pin => {
        map.removeLayer(pin);
    });
    portPins = [];

    // Clear all waypoint pins
    waypointPins.forEach(pin => {
        map.removeLayer(pin);
    });
    waypointPins = [];

    // Reset route points array
    routePoints = [];

    // Reset route segments array
    routeSegments = [];

    // Reset waypoint selection
    isWaypointSelectionActive = false;
    map.getContainer().style.cursor = '';

    // Reset the map view to the initial state
    map.setView([20, 60], 3);
}

// Get all pins currently on the map
function getAllPins() {
    return {
        departure: departurePin ? [departurePin] : [],
        arrival: arrivalPin ? [arrivalPin] : [],
        clicked: clickedPins,
        port: portPins,
        waypoint: waypointPins
    };
}

// Function to get the current route data
function getRouteData() {
    return routePoints;
}

// Zoom animation for when a point is added
function zoomInThenOut(lat, lon) {
    // First zoom in to the point
    map.flyTo([lat, lon], 12, { duration: 1 });

    // Then zoom out after a short delay
    setTimeout(() => {
        // Calculate bounds to include all points
        let bounds;

        if (routePoints.length >= 2) {
            // If we have multiple points, create bounds based on all route points
            bounds = L.latLngBounds(routePoints.map(p => p.latLng));
            // Add some padding to the bounds
            bounds = bounds.pad(0.3);
        } else {
            // If we have only one point, create a view centered on that point but zoomed out
            bounds = L.latLngBounds([
                [lat - 10, lon - 10],
                [lat + 10, lon + 10]
            ]);
        }

        // Calculate padding for the info panel
        const paddingLeft = window.innerWidth * 0.45; // Adjust for overlay width

        // Fly to the bounds with appropriate padding
        map.flyToBounds(bounds, {
            paddingTopLeft: [paddingLeft, 20],
            paddingBottomRight: [20, 20],
            duration: 1.5
        });
    }, 1500); // Delay of 1.5 seconds before zooming out
}

function removePin(pinType, index) {
    try {
        // Handle different pin types
        if (pinType === 'departure') {
            if (departurePin) {
                map.removeLayer(departurePin);
                departurePin = null;
                routePoints = routePoints.filter(p => p.type !== 'departure');
            }
        } else if (pinType === 'arrival') {
            if (arrivalPin) {
                map.removeLayer(arrivalPin);
                arrivalPin = null;
                routePoints = routePoints.filter(p => p.type !== 'arrival');
            }
        } else if (pinType === 'waypoint') {
            // Remove waypoint pin by index if index is provided
            if (index !== undefined && index >= 0 && index < waypointPins.length) {
                // Remove from map
                map.removeLayer(waypointPins[index]);

                // Remove from arrays
                waypointPins.splice(index, 1);

                // Remove from clicked pins array if it exists there
                const waypointLatLng = routePoints.find(p => p.type === 'waypoint' && p.index === index)?.latLng;
                if (waypointLatLng) {
                    const clickedIndex = clickedPins.findIndex(pin => {
                        const pinLatLng = pin.getLatLng();
                        return Math.abs(pinLatLng.lat - waypointLatLng[0]) < 0.0001 &&
                            Math.abs(pinLatLng.lng - waypointLatLng[1]) < 0.0001;
                    });

                    if (clickedIndex !== -1) {
                        map.removeLayer(clickedPins[clickedIndex]);
                        clickedPins.splice(clickedIndex, 1);
                    }
                }

                // Filter out the waypoint from route points
                routePoints = routePoints.filter(p => !(p.type === 'waypoint' && p.index === index));

                // Update indexes for remaining waypoints
                let waypointCounter = 0;
                routePoints.forEach(p => {
                    if (p.type === 'waypoint') {
                        p.index = waypointCounter++;
                    }
                });
            }
        } else if (pinType === 'port') {
            // Remove port pin by index if index is provided
            if (index !== undefined && index >= 0 && index < portPins.length) {
                // Remove from map
                map.removeLayer(portPins[index]);

                // Remove from arrays
                portPins.splice(index, 1);

                // Filter out the port from route points
                routePoints = routePoints.filter(p => !(p.type === 'port' && p.index === index));

                // Update indexes for remaining ports
                let portCounter = 0;
                routePoints.forEach(p => {
                    if (p.type === 'port') {
                        p.index = portCounter++;
                    }
                });
            }
        }

        // Reorganize route points and recalculate route if we have sufficient points
        reorganizeRoutePoints();

        // Notify Blazor that a point was removed
        if (currentDotNetHelper && departurePin && arrivalPin) {
            currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
        } else if (currentDotNetHelper) {
            currentDotNetHelper.invokeMethodAsync('UpdateRoutePoints', getRoutePointsJson());
        }

        // Clear route if we don't have both departure and arrival
        if (!departurePin || !arrivalPin) {
            routeLayer.clearLayers();
            routeSegments = [];
        }

        return true;
    } catch (error) {
        console.error(`Error removing ${pinType} pin:`, error);
        return false;
    }
}

// Function to handle removal from Blazor side
function removeWaypoint(latitude, longitude) {
    try {
        // Find the waypoint in route points by coordinates
        const waypointIndex = routePoints.findIndex(p =>
            p.type === 'waypoint' &&
            Math.abs(p.latLng[0] - latitude) < 0.0001 &&
            Math.abs(p.latLng[1] - longitude) < 0.0001
        );

        if (waypointIndex !== -1) {
            // Get the array index of the waypoint
            const waypointArrayIndex = waypointPins.findIndex(pin => {
                const pinLatLng = pin.getLatLng();
                return Math.abs(pinLatLng.lat - latitude) < 0.0001 &&
                    Math.abs(pinLatLng.lng - longitude) < 0.0001;
            });

            if (waypointArrayIndex !== -1) {
                return removePin('waypoint', waypointArrayIndex);
            }
        }
        return false;
    } catch (error) {
        console.error("Error removing waypoint:", error);
        return false;
    }
}

// Function to handle removal of port from Blazor side
function removePort(portName, latitude, longitude) {
    try {
        // Find the port in route points by name or coordinates
        const portIndex = routePoints.findIndex(p =>
            p.type === 'port' &&
            (p.name === portName ||
                (Math.abs(p.latLng[0] - latitude) < 0.0001 &&
                    Math.abs(p.latLng[1] - longitude) < 0.0001))
        );

        if (portIndex !== -1) {
            // Get the array index of the port
            const portArrayIndex = portPins.findIndex(pin => {
                const pinLatLng = pin.getLatLng();
                return (pin.getPopup().getContent().includes(portName) ||
                    (Math.abs(pinLatLng.lat - latitude) < 0.0001 &&
                        Math.abs(pinLatLng.lng - longitude) < 0.0001));
            });

            if (portArrayIndex !== -1) {
                return removePin('port', portArrayIndex);
            }
        }
        return false;
    } catch (error) {
        console.error("Error removing port:", error);
        return false;
    }
}