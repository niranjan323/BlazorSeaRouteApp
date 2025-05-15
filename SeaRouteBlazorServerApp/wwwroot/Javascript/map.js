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

// Debounce function to limit the rate of function execution
function debounce(func, wait) {
    let timeout;
    return function (...args) {
        const context = this;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}

// Cache for location searches to prevent redundant API calls
const locationCache = new Map();

function initializeMap(dotNetHelper) {
    // Check if map already exists
    if (map) {
        console.log('Map already initialized');
        return;
    }

    currentDotNetHelper = dotNetHelper;
    map = L.map('map', {
        // Disable zoom animation for better performance
        zoomAnimation: false,
        // Optimize for mobile 
        preferCanvas: true
    }).setView([20, 60], 3);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        crossOrigin: 'anonymous',
        // Improve tile loading performance
        updateWhenIdle: true,
        updateWhenZooming: false,
    }).addTo(map);

    // Initialize the route layer
    routeLayer = L.layerGroup().addTo(map);

    // Create coordinate tooltip only once
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

    // Use optimized event handlers with debouncing
    map.on('mousemove', debounce(function (e) {
        if (isWaypointSelectionActive) {
            const { lat, lng } = e.latlng;
            coordTooltip.textContent = `Click to set waypoint\nLat: ${lat.toFixed(4)}, Lng: ${lng.toFixed(4)}`;
            coordTooltip.style.display = 'block';
            coordTooltip.style.left = (e.originalEvent.clientX + 15) + 'px';
            coordTooltip.style.top = (e.originalEvent.clientY + 15) + 'px';
        }
    }, 30)); // 30ms debounce for smoother tooltips

    map.getContainer().addEventListener('mouseleave', function () {
        coordTooltip.style.display = 'none';
    });

    // Optimize click event by removing redundant operations
    map.on('click', function (e) {
        if (isWaypointSelectionActive) {
            const latitude = e.latlng.lat;
            const longitude = e.latlng.lng;

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

            // Add to route points as a waypoint
            routePoints.push({
                type: 'waypoint',
                latLng: [latitude, longitude],
                name: `Waypoint ${waypointPins.length}`
            });

            // Use optimized zoom function
            zoomInThenOut(latitude, longitude);

            // Capture coordinates in Blazor
            currentDotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);

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
    });
}

function canCalculateRoute() {
    // We need at least a departure point and one other point (arrival, port, or waypoint)
    return departurePin && (arrivalPin || portPins.length > 0 || waypointPins.length > 0);
}

function setWaypointSelection(active) {
    isWaypointSelectionActive = active;

    // Update cursor style once
    map.getContainer().style.cursor = active ? 'crosshair' : '';

    // Only update tooltip display if needed
    if (!active && coordTooltip.style.display !== 'none') {
        coordTooltip.style.display = 'none';
    }
}

// Function to search for locations and add pins with caching
async function searchLocation(query, isDeparture) {
    try {
        // Use cache to avoid redundant API calls
        const cacheKey = `${query}-${isDeparture}`;

        let data;
        if (locationCache.has(cacheKey)) {
            data = locationCache.get(cacheKey);
        } else {
            // Add a short timeout to prevent rapid successive calls
            await new Promise(resolve => setTimeout(resolve, 50));

            const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`);
            data = await response.json();

            // Cache the result for future use
            if (data.length > 0) {
                locationCache.set(cacheKey, data);
            }
        }

        if (data.length > 0) {
            const lat = parseFloat(data[0].lat);
            const lon = parseFloat(data[0].lon);

            // Add pin based on type (departure or arrival)
            if (isDeparture) {
                // Remove previous departure pin if exists
                if (departurePin) {
                    map.removeLayer(departurePin);
                }

                // Create new departure pin with optimized marker
                departurePin = L.marker([lat, lon], {
                    shadowPane: false,
                    // Avoid rasterization to improve performance
                    bubblingMouseEvents: true
                }).addTo(map);

                departurePin.bindPopup("Departure: " + query);

                // Update route points efficiently
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

                // Optimize zooming
                zoomInThenOut(lat, lon);
            } else {
                // Remove previous arrival pin if exists
                if (arrivalPin) {
                    map.removeLayer(arrivalPin);
                }

                // Create new arrival pin with optimized marker
                arrivalPin = L.marker([lat, lon], {
                    shadowPane: false,
                    bubblingMouseEvents: true
                }).addTo(map);

                arrivalPin.bindPopup("Arrival: " + query);

                // Update route points efficiently
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

                // Optimize zooming
                zoomInThenOut(lat, lon);
            }

            // Reorganize route points
            reorganizeRoutePoints();

            // Check if we can calculate a route and do it only if needed
            if (canCalculateRoute()) {
                // Use requestAnimationFrame for smoother UI updates
                window.requestAnimationFrame(() => {
                    currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
                });
            }
        } else {
            console.warn("Port not found:", query);
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Function to add a port based on API search results with caching
async function zoomAndPinLocation(query, isDeparture, lat = null, lon = null) {
    try {
        if (lat === null || lon === null) {
            // Use cache if available
            const cacheKey = `port-${query}`;

            let data;
            if (locationCache.has(cacheKey)) {
                data = locationCache.get(cacheKey);
            } else {
                const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`);
                data = await response.json();

                // Cache the result for future use
                if (data.length > 0) {
                    locationCache.set(cacheKey, data);
                }
            }

            if (data.length > 0) {
                lat = parseFloat(data[0].lat);
                lon = parseFloat(data[0].lon);
            } else {
                console.warn("Port not found:", query);
                return;
            }
        }

        // Add pin and store it in the array - optimize marker creation
        const newPin = L.marker([lat, lon], {
            shadowPane: false,
            bubblingMouseEvents: true
        }).addTo(map);

        newPin.bindPopup(`Intermediate Port: ${query}`);
        portPins.push(newPin);

        // Add to route points as an intermediate port
        routePoints.push({
            type: 'port',
            latLng: [lat, lon],
            name: query
        });

        reorganizeRoutePoints();
        zoomInThenOut(lat, lon);

        // Check if we can calculate a route
        if (canCalculateRoute()) {
            // Use requestAnimationFrame for smoother UI updates
            window.requestAnimationFrame(() => {
                currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
            });
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Ensure route points are properly organized - optimized version
function reorganizeRoutePoints() {
    // Extract departure and arrival points
    const departurePoint = routePoints.find(p => p.type === 'departure');
    const arrivalPoint = routePoints.find(p => p.type === 'arrival');
    const otherPoints = routePoints.filter(p => p.type !== 'departure' && p.type !== 'arrival');

    // Use efficient array manipulation
    routePoints = departurePoint ? [departurePoint] : [];
    routePoints = routePoints.concat(otherPoints);
    if (arrivalPoint) routePoints.push(arrivalPoint);

    return routePoints;
}

// Function to create a segment route from FastAPI response - optimized
function createSeaRoutefromAPI(routeJson) {
    try {
        // Parse the GeoJSON string if it's not already an object
        const routeData = typeof routeJson === 'string' ? JSON.parse(routeJson) : routeJson;

        // Extract LineString coordinates from the GeoJSON
        const geoJsonCoordinates = routeData.geometry.coordinates;

        // Optimize coordinate conversion for Leaflet
        // Use a more efficient way to convert coordinates
        const leafletCoordinates = new Array(geoJsonCoordinates.length);
        for (let i = 0; i < geoJsonCoordinates.length; i++) {
            leafletCoordinates[i] = [geoJsonCoordinates[i][1], geoJsonCoordinates[i][0]];
        }

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

// Process route segment - optimized
function processRouteSegment(routeJson, segmentIndex, totalSegments) {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Store this segment
        routeSegments[segmentIndex] = segment;

        // If all segments are received, draw the combined route
        if (routeSegments.filter(s => s !== null).length === totalSegments) {
            // Use requestAnimationFrame for smoother rendering
            window.requestAnimationFrame(() => {
                drawCombinedRoute(routeSegments);
            });
        }

        return segment;
    } catch (error) {
        console.error("Error processing route segment:", error);
        return null;
    }
}

// Draw combined route from multiple segments - optimized
function drawCombinedRoute(segments) {
    // Clear previous route
    routeLayer.clearLayers();

    if (!segments || segments.length === 0) {
        return;
    }

    // Pre-allocate arrays for better performance
    const allCoordinates = [];
    const segmentBoundaries = [];
    let totalDistance = 0;
    let totalDuration = 0;

    // Process segments more efficiently
    segments.forEach((segment, index) => {
        if (segment && segment.coordinates) {
            const startIndex = allCoordinates.length;

            // Use push.apply for faster array concatenation
            Array.prototype.push.apply(allCoordinates, segment.coordinates);

            segmentBoundaries.push({
                startIndex: startIndex,
                endIndex: allCoordinates.length - 1,
                properties: segment.properties,
                segmentIndex: index
            });

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

    // Create route polyline with optimized options
    const routePolyline = L.polyline(allCoordinates, {
        color: '#0066ff',
        weight: 3,
        opacity: 0.8,
        smoothFactor: 1,
        // Improve performance by reducing points on zoom
        interactive: false
    }).addTo(routeLayer);

    // Add total distance and duration marker only when needed
    if (routePoints.length > 2 && allCoordinates.length > 0) {
        const midIndex = Math.floor(allCoordinates.length / 2);
        const midPoint = allCoordinates[midIndex];

        // Create marker with precomputed HTML for better performance
        const distanceMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
         Distance: ${totalDistance} km<br>
         Duration: ${totalDuration} hours
        </div>`;

        L.marker(midPoint, {
            icon: L.divIcon({
                className: 'distance-marker',
                html: distanceMarkerHtml,
                iconSize: null
            })
        }).addTo(routeLayer);
    }

    // Add distance and duration markers for each segment - optimize for performance
    // Only add detailed markers if we have fewer than 10 segments to improve performance
    if (segmentBoundaries.length < 10) {
        segmentBoundaries.forEach((boundary, idx) => {
            const segmentMidIndex = Math.floor((boundary.startIndex + boundary.endIndex) / 2);
            const segmentMidPoint = allCoordinates[segmentMidIndex];

            const segmentProps = boundary.properties;
            const segmentDistance = segmentProps && segmentProps.length ? Math.round(segmentProps.length) : 0;
            const segmentDuration = segmentProps && segmentProps.duration_hours ? Math.round(segmentProps.duration_hours) : 0;

            const fromPoint = routePoints[idx];
            const toPoint = routePoints[idx + 1];
            const fromName = fromPoint ? fromPoint.name || 'Point' : 'Point';
            const toName = toPoint ? toPoint.name || 'Point' : 'Point';

            // Segment marker with precomputed HTML
            const segmentMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
                Distance: ${segmentDistance} km<br>
                Duration: ${segmentDuration} hours
            </div>`;

            const segmentMarker = L.marker(segmentMidPoint, {
                icon: L.divIcon({
                    className: 'distance-marker',
                    html: segmentMarkerHtml,
                    iconSize: null
                })
            }).addTo(routeLayer);

            // Use efficient tooltip creation
            segmentMarker.bindTooltip(`${fromName} → ${toName}
${segmentDistance} km / ${segmentDuration} hours`,
                { permanent: false, direction: 'top', offset: [0, -10] });
        });
    }

    // Calculate bounds more efficiently
    const routeBounds = L.latLngBounds(allCoordinates);
    const paddingLeft = window.innerWidth * 0.45;

    // Use an optimized flyToBounds that's less demanding
    map.flyToBounds(routeBounds, {
        paddingTopLeft: [paddingLeft, 20],
        paddingBottomRight: [20, 20],
        duration: 1.5
    });

    return routePolyline;
}

// Update map with port data - optimized
function updateMapWithPortData(portData, isDeparture) {
    if (!portData || !portData.latitude || !portData.longitude) return;

    const lat = parseFloat(portData.latitude);
    const lon = parseFloat(portData.longitude);
    const name = portData.name || "Port";
    const unlocode = portData.unlocode || "";

    if (isNaN(lat) || isNaN(lon)) return;

    // Create the marker with optimized options
    const marker = L.marker([lat, lon], {
        shadowPane: false,
        bubblingMouseEvents: true
    }).addTo(map);

    marker.bindPopup(`${name} (${unlocode})<br>Lat: ${lat.toFixed(4)}, Lng: ${lon.toFixed(4)}`);

    // Add to appropriate array
    if (isDeparture) {
        if (departurePin) {
            map.removeLayer(departurePin);
        }
        departurePin = marker;

        // Update route points efficiently
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

        // Add to route points efficiently
        routePoints.push({
            type: 'port',
            latLng: [lat, lon],
            name: name
        });
    }

    // Use the optimized zoom pattern
    zoomInThenOut(lat, lon);

    // Trigger route recalculation more efficiently
    if (canCalculateRoute()) {
        reorganizeRoutePoints();
        // Use requestAnimationFrame for smoother updates
        window.requestAnimationFrame(() => {
            currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
        });
    }
}

// Add arrival port data - optimized
function addArrivalPort(portData) {
    if (!portData || !portData.latitude || !portData.longitude) return;

    const lat = parseFloat(portData.latitude);
    const lon = parseFloat(portData.longitude);
    const name = portData.name || "Arrival Port";
    const unlocode = portData.unlocode || "";

    if (isNaN(lat) || isNaN(lon)) return;

    // Create the marker with optimized options
    const marker = L.marker([lat, lon], {
        shadowPane: false,
        bubblingMouseEvents: true
    }).addTo(map);

    marker.bindPopup(`Arrival: ${name} (${unlocode})<br>Lat: ${lat.toFixed(4)}, Lng: ${lon.toFixed(4)}`);

    // Set as arrival pin efficiently
    if (arrivalPin) {
        map.removeLayer(arrivalPin);
    }
    arrivalPin = marker;

    // Update route points efficiently
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

    // Use the optimized zoom pattern
    zoomInThenOut(lat, lon);

    // Trigger route recalculation more efficiently
    if (canCalculateRoute()) {
        reorganizeRoutePoints();
        // Use requestAnimationFrame for smoother updates
        window.requestAnimationFrame(() => {
            currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
        });
    }
}

// Modified function to initialize a multi-segment route calculation - optimized
function initializeRouteCalculation() {
    // Clear previous route segments efficiently
    routeSegments = [];

    // Clear previous route layer efficiently
    routeLayer.clearLayers();

    // Return the currently organized route points
    return reorganizeRoutePoints();
}

// Reset all map data - optimized
function resetMap() {
    // Clear all pins and layers
    if (departurePin) {
        map.removeLayer(departurePin);
        departurePin = null;
    }
    if (arrivalPin) {
        map.removeLayer(arrivalPin);
        arrivalPin = null;
    }
    if (routeLayer) {
        routeLayer.clearLayers();
    }
    
    // Clear all waypoint pins
    clickedPins.forEach(pin => {
        if (pin) map.removeLayer(pin);
    });
    clickedPins = [];
    
    portPins.forEach(pin => {
        if (pin) map.removeLayer(pin);
    });
    portPins = [];
    
    waypointPins.forEach(pin => {
        if (pin) map.removeLayer(pin);
    });
    waypointPins = [];
    
    // Clear route points and segments
    routePoints = [];
    routeSegments = [];
    
    // Reset waypoint selection
    isWaypointSelectionActive = false;
    if (map) {
        map.getContainer().style.cursor = '';
        // Reset the map view to the initial state
        map.setView([20, 60], 3);
    }
}

// Get all pins currently on the map - unchanged
function getAllPins() {
    return {
        departure: departurePin ? [departurePin] : [],
        arrival: arrivalPin ? [arrivalPin] : [],
        clicked: clickedPins,
        port: portPins,
        waypoint: waypointPins
    };
}

// Function to get the current route data - unchanged
function getRouteData() {
    return routePoints;
}

// Optimized zoom animation for when a point is added
function zoomInThenOut(lat, lon) {
    // Skip animation if another animation is in progress
    if (map._animatingZoom) return;

    // First zoom in to the point with faster animation
    map.flyTo([lat, lon], 12, { duration: 0.8 });

    // Then zoom out after a shorter delay
    setTimeout(() => {
        // Avoid processing if map has been reset
        if (!map) return;

        // Calculate bounds to include all points
        let bounds;

        if (routePoints.length >= 2) {
            // Create bounds more efficiently
            bounds = L.latLngBounds(routePoints.map(p => p.latLng));
            bounds = bounds.pad(0.3);
        } else {
            // Simple bounds for single point
            bounds = L.latLngBounds([
                [lat - 10, lon - 10],
                [lat + 10, lon + 10]
            ]);
        }

        // Calculate padding for the info panel
        const paddingLeft = window.innerWidth * 0.45;

        // Use flyToBounds with shorter duration
        map.flyToBounds(bounds, {
            paddingTopLeft: [paddingLeft, 20],
            paddingBottomRight: [20, 20],
            duration: 1 // Shorter duration for better responsiveness
        });
    }, 1000); // Reduced delay from 1500ms to 1000ms
}

// Optimized pin removal function
function removePin(pinType, index) {
    try {
        // Use a more efficient approach based on pin type
        switch (pinType) {
            case 'departure':
                if (departurePin) {
                    map.removeLayer(departurePin);
                    departurePin = null;
                    routePoints = routePoints.filter(p => p.type !== 'departure');
                }
                break;

            case 'arrival':
                if (arrivalPin) {
                    map.removeLayer(arrivalPin);
                    arrivalPin = null;
                    routePoints = routePoints.filter(p => p.type !== 'arrival');
                }
                break;

            case 'waypoint':
                if (index !== undefined && index >= 0 && index < waypointPins.length) {
                    // Remove pin from map
                    map.removeLayer(waypointPins[index]);

                    // Get coordinates before removing from array
                    const waypointLatLng = routePoints.find(p =>
                        p.type === 'waypoint' && p.index === index)?.latLng;

                    // Remove from arrays efficiently
                    waypointPins.splice(index, 1);

                    // Remove from clicked pins if present
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

                    // Filter route points efficiently
                    routePoints = routePoints.filter(p => !(p.type === 'waypoint' && p.index === index));

                    // Update waypoint indexes more efficiently
                    let waypointCounter = 0;
                    routePoints.forEach(p => {
                        if (p.type === 'waypoint') {
                            p.index = waypointCounter++;
                        }
                    });
                }
                break;

            case 'port':
                if (index !== undefined && index >= 0 && index < portPins.length) {
                    // Remove pin from map
                    map.removeLayer(portPins[index]);

                    // Remove from array
                    portPins.splice(index, 1);

                    // Filter route points efficiently
                    routePoints = routePoints.filter(p => !(p.type === 'port' && p.index === index));

                    // Update port indexes efficiently
                    let portCounter = 0;
                    routePoints.forEach(p => {
                        if (p.type === 'port') {
                            p.index = portCounter++;
                        }
                    });
                }
                break;
        }

        // Reorganize route points efficiently
        reorganizeRoutePoints();

        // Notify Blazor that a point was removed - use requestAnimationFrame for smoother updates
        if (currentDotNetHelper) {
            window.requestAnimationFrame(() => {
                if (departurePin && arrivalPin) {
                    currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
                } else {
                    currentDotNetHelper.invokeMethodAsync('UpdateRoutePoints', JSON.stringify(routePoints));
                }
            });
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

// Optimized waypoint removal function
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

function destroyMap() {
    if (map) {
        // Remove all layers and event listeners
        map.eachLayer(function(layer) {
            map.removeLayer(layer);
        });
        
        // Remove the map instance
        map.remove();
        map = null;
        
        // Clear all references
        departurePin = null;
        arrivalPin = null;
        routeLayer = null;
        isWaypointSelectionActive = false;
        coordTooltip = null;
        currentDotNetHelper = null;
        clickedPins = [];
        portPins = [];
        waypointPins = [];
        routePoints = [];
        routeSegments = [];
    }
}