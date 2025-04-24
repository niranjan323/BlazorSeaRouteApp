// Global variables for map functionality
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
async function zoomAndPinLocation(query, isDeparture) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();

        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);

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

            // Reorganize route points to keep departure first and arrival last
            reorganizeRoutePoints();

            // Use the new zoom pattern
            zoomInThenOut(lat, lon);

            // Trigger route recalculation if we have departure and arrival
            if (departurePin && arrivalPin) {
                currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
            }
        } else {
            alert("Port not found!");
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

// Draw combined route from multiple segments
function drawCombinedRoute(segments) {
    // Clear previous route
    routeLayer.clearLayers();

    if (!segments || segments.length === 0) {
        return;
    }

    // Combine all coordinates from all segments
    let allCoordinates = [];
    let totalDistance = 0;
    let totalDuration = 0;

    segments.forEach(segment => {
        if (segment && segment.coordinates) {
            allCoordinates = allCoordinates.concat(segment.coordinates);

            // Sum up properties if available
            if (segment.properties) {
                if (segment.properties.length) {
                    totalDistance += Math.round(segment.properties.length);
                }
                if (segment.properties.duration_hours) {
                    totalDuration += Math.round(segment.properties.duration_hours);
                }
            }
        }
    });

    // Create the polyline with the combined coordinates
    const routePolyline = L.polyline(allCoordinates, {
        color: '#0066ff',
        weight: 3,
        opacity: 0.8,
        smoothFactor: 1,
        dashArray: null
    }).addTo(routeLayer);

    // Add total distance and duration marker at the middle of the route
    if (allCoordinates.length > 0) {
        const midIndex = Math.floor(allCoordinates.length / 2);
        const midPoint = allCoordinates[midIndex];

        L.marker(midPoint, {
            icon: L.divIcon({
                className: 'distance-marker',
                html: `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
                        Distance: ${totalDistance} km<br>
                        Duration: ${totalDuration} hours
                       </div>`,
                iconSize: null
            })
        }).addTo(routeLayer);
    }

    // Calculate bounds to fit the entire route
    const routeBounds = L.latLngBounds(allCoordinates);

    // Calculate pixel padding - approximately 40% of map width to the left for your overlay
    const paddingLeft = window.innerWidth * 0.45;

    // Fit the map to the route bounds with appropriate padding
    map.flyToBounds(routeBounds, {
        paddingTopLeft: [paddingLeft, 20],
        paddingBottomRight: [20, 20],
        duration: 1.5
    });

    return routePolyline;
}

// Process a single route segment from FastAPI
function processRouteSegment(routeJson, segmentIndex, totalSegments) {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Store this segment
        routeSegments[segmentIndex] = segment;

        // If all segments are received, draw the combined route
        if (routeSegments.filter(s => s !== null).length === totalSegments) {
            drawCombinedRoute(routeSegments);
        }

        return segment;
    } catch (error) {
        console.error("Error processing route segment:", error);
        return null;
    }
}

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