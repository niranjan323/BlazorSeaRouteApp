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
    //if (map) {
    //    console.log("Map already initialized")
    //    return;
    //}
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
async function searchLocation(query, isDeparture, lat = null, lon = null) {
    try {
        // If lat and lon are provided, use them directly
        if (lat !== null && lon !== null) {
            // Convert to float to ensure proper handling
            lat = parseFloat(lat);
            lon = parseFloat(lon);

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

            return;
        }

        // If coordinates are not provided, continue with API search
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
        // If lat and lon are provided, use them directly
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
        } else {
            // Convert to float to ensure proper handling
            lat = parseFloat(lat);
            lon = parseFloat(lon);
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

// Helper: Normalize longitude to [-180, 180) // Modified By Niranjan
function normalizeLongitude(lon) {
    const T = 360.0, t0 = -180.0;
    let k = Math.floor((lon - t0) / T);
    let alpha0 = lon - k * T;
    if (alpha0 >= 180.0) alpha0 -= T;
    return alpha0;
}

// Helper: Transform segment for continuity // Modified By Niranjan
function transformSegmentForContinuity(currentSegment, previousSegment) {
    if (!previousSegment || previousSegment.length === 0 || !currentSegment || currentSegment.length === 0) {
        return currentSegment;
    }
    const p1 = previousSegment[previousSegment.length - 1][1]; // longitude of last point in previous segment
    const p2 = currentSegment[0][1]; // longitude of first point in current segment
    const T = 360.0;
    const k = Math.floor((p1 - p2) / T);
    return currentSegment.map(coord => [coord[0], coord[1] + k * T]);
}

// Create segment from API, only normalize if first segment // Modified By Niranjan
function createSeaRoutefromAPI(routeJson, shouldNormalize = false) {
    try {
        const routeData = typeof routeJson === 'string' ? JSON.parse(routeJson) : routeJson;
        const geoJsonCoordinates = routeData.geometry.coordinates;
        const leafletCoordinates = geoJsonCoordinates.map(coord => {
            let lng = coord[0], lat = coord[1];
            if (shouldNormalize) lng = normalizeLongitude(lng);
            return [lat, lng];
        });
        return {
            coordinates: leafletCoordinates,
            properties: routeData.properties
        };
    } catch (error) {
        console.error("Error creating sea route segment:", error);
        return null;
    }
}

// In processRouteSegment, pass shouldNormalize for the first segment // Modified By Niranjan
function processRouteSegment(routeJson, segmentIndex, totalSegments) {
    try {
        const shouldNormalize = segmentIndex === 0;
        let segment = createSeaRoutefromAPI(routeJson, shouldNormalize);
        if (segmentIndex > 0 && routeSegments[segmentIndex - 1]) {
            const previousSegment = routeSegments[segmentIndex - 1];
            if (previousSegment && previousSegment.coordinates) {
                segment.coordinates = transformSegmentForContinuity(segment.coordinates, previousSegment.coordinates);
            }
        }
        routeSegments[segmentIndex] = segment;
        if (routeSegments.filter(s => s !== null).length === totalSegments) {
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

// Modified By Niranjan: Ensure continuity in drawCombinedRoute
function drawCombinedRoute(segments) {
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
            // Check for continuity with previous segment
            if (index > 0 && allCoordinates.length > 0) {
                const lastCoord = allCoordinates[allCoordinates.length - 1];
                const firstCoord = segment.coordinates[0];
                const distance = Math.sqrt(
                    Math.pow(lastCoord[0] - firstCoord[0], 2) +
                    Math.pow(lastCoord[1] - firstCoord[1], 2)
                );
                // If there's a significant gap (> 0.01 degrees), add a connecting line
                if (distance > 0.01) {
                    const numInterpolationPoints = Math.min(5, Math.floor(distance * 100));
                    for (let i = 1; i <= numInterpolationPoints; i++) {
                        const t = i / (numInterpolationPoints + 1);
                        const interpolatedLat = lastCoord[0] + t * (firstCoord[0] - lastCoord[0]);
                        const interpolatedLng = lastCoord[1] + t * (firstCoord[1] - lastCoord[1]);
                        allCoordinates.push([interpolatedLat, interpolatedLng]);
                    }
                }
            }
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
        //const distanceMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
        // Distance: ${totalDistance} nm<br>
        // Duration: ${totalDuration} hours
        //</div>`;

        //L.marker(midPoint, {
        //    icon: L.divIcon({
        //        className: 'distance-marker',
        //        html: distanceMarkerHtml,
        //        iconSize: null
        //    })
        //}).addTo(routeLayer);
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
            //            const segmentMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
            //                Distance: ${segmentDistance} nm<br>
            //                Duration: ${segmentDuration} hours
            //            </div>`;

            //            const segmentMarker = L.marker(segmentMidPoint, {
            //                icon: L.divIcon({
            //                    className: 'distance-marker',
            //                    html: segmentMarkerHtml,
            //                    iconSize: null
            //                })
            //            }).addTo(routeLayer);

            // Use efficient tooltip creation
            //            segmentMarker.bindTooltip(`${fromName} → ${toName}
            //${segmentDistance} nm / ${segmentDuration} hours`,
            //                { permanent: false, direction: 'top', offset: [0, -10] });
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
    // Clear departure and arrival pins efficiently
    if (departurePin) {
        map.removeLayer(departurePin);
        departurePin = null;
    }
    if (arrivalPin) {
        map.removeLayer(arrivalPin);
        arrivalPin = null;
    }

    // Clear route layer efficiently
    routeLayer.clearLayers();

    // Remove all pins efficiently in bulk operations
    // Remove all clicked pins
    clickedPins.forEach(pin => map.removeLayer(pin));
    clickedPins = [];

    // Remove all port pins
    portPins.forEach(pin => map.removeLayer(pin));
    portPins = [];

    // Remove all waypoint pins
    waypointPins.forEach(pin => map.removeLayer(pin));
    waypointPins = [];

    // Reset data structures efficiently
    routePoints = [];
    routeSegments = [];
    isWaypointSelectionActive = false;
    map.getContainer().style.cursor = '';

    // Reset the map view efficiently - use setView instead of flyTo for better performance
    map.setView([20, 60], 3, { animate: false });

    // Clear the location cache to prevent stale data
    locationCache.clear();
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

                    const ports = routePoints.filter(p => (p.type === 'port'));
                    const portItem = ports[index];

                    // Filter route points efficiently
                    //routePoints = routePoints.filter(p => !(p.type === 'port' && p.index === index));
                    routePoints = routePoints.filter(p => !(p == portItem));

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
                    //currentDotNetHelper.invokeMethodAsync('UpdateRoutePoints', JSON.stringify(routePoints));
                    currentDotNetHelper.invokeMethodAsync('RecalculateRoute');
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


function createSeaRoutefromAPIForShort(routeJson) {
    try {
        // Parse the GeoJSON string if it's not already an object
        const routeData = typeof routeJson === 'string' ? JSON.parse(routeJson) : routeJson;

        // Extract LineString coordinates from the GeoJSON
        const geoJsonCoordinates = routeData.geometry.coordinates;

        // Convert from [longitude, latitude] to [latitude, longitude] for Leaflet
        const leafletCoordinates = geoJsonCoordinates.map(coord => [coord[1], coord[0]]);

        // Clear previous route
        if (routeLayer) {
            routeLayer.clearLayers();
        }

        // Create the polyline with the converted coordinates
        const seaRoutePolyline = L.polyline(leafletCoordinates, {
            color: '#0066ff',
            weight: 3,
            opacity: 0.8,
            smoothFactor: 1,
            dashArray: null
        }).addTo(routeLayer);

        // Get start and end points for markers
        const startPoint = leafletCoordinates[0];
        const endPoint = leafletCoordinates[leafletCoordinates.length - 1];

        // Add markers for origin and destination if they don't exist yet
        const properties = routeData.properties;

        if (properties && properties.port_origin) {
            // Check if departure pin already exists and remove it if it does
            if (departurePin) {
                map.removeLayer(departurePin);
            }

            // Create departure pin
            departurePin = L.marker(startPoint).addTo(map);
            departurePin.bindPopup(`Departure: ${properties.port_origin.name} (${properties.port_origin.port})`).openPopup();
        }

        if (properties && properties.port_dest) {
            // Check if arrival pin already exists and remove it if it does
            if (arrivalPin) {
                map.removeLayer(arrivalPin);
            }

            // Create arrival pin
            arrivalPin = L.marker(endPoint).addTo(map);
            arrivalPin.bindPopup(`Arrival: ${properties.port_dest.name} (${properties.port_dest.port})`).openPopup();
        }

        // Add distance information
        if (properties && properties.length) {
            // Add total distance marker at the middle of the route
            const midIndex = Math.floor(leafletCoordinates.length / 2);
            const midPoint = leafletCoordinates[midIndex];

            L.marker(midPoint, {
                icon: L.divIcon({
                    className: 'distance-marker',
                    html: `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
                            Distance: ${Math.round(properties.length)} nm<br>
                            Duration: ${Math.round(properties.duration_hours)} hours
                           </div>`,
                    iconSize: null
                })
            }).addTo(routeLayer);
        }

        // Calculate bounds to fit the entire route
        const routeBounds = L.latLngBounds(leafletCoordinates);

        // Calculate pixel padding - approximately 40% of map width to the left for your overlay
        const paddingLeft = window.innerWidth * 0.45;

        // Fit the map to the route bounds with appropriate padding
        map.flyToBounds(routeBounds, {
            paddingTopLeft: [paddingLeft, 20],
            paddingBottomRight: [20, 20],
            duration: 1.5
        });

        // Store the route points for potential later use
        routePoints = [];

        // Add departure point
        if (properties && properties.port_origin) {
            routePoints.push({
                type: 'departure',
                latLng: startPoint,
                name: properties.port_origin.name
            });
        }

        // Add arrival point
        if (properties && properties.port_dest) {
            routePoints.push({
                type: 'arrival',
                latLng: endPoint,
                name: properties.port_dest.name
            });
        }

        return seaRoutePolyline;
    } catch (error) {
        console.error("Error creating sea route:", error);
        return null;
    }
}
// graph-1
window.drawChart = (canvasid, reductionFactorData) => {
    console.log("Draw chart called for canvas:", canvasid);
    console.log("Received data type:", typeof reductionFactorData);
    console.log("Received data:", reductionFactorData);

    // Make sure Chart is defined
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded!");
        return;
    }

    // Get canvas element
    const canvas = document.getElementById(canvasid);
    if (!canvas) {
        console.error(`Canvas element with id '${canvasid}' not found!`);
        return;
    }

    console.log("Canvas found:", canvas);

    // Get the 2D context
    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("Could not get 2D context from canvas!");
        return;
    }

    // Parse the reduction factor data if it's a string
    let reductionFactor;
    try {
        if (typeof reductionFactorData === 'string') {
            reductionFactor = JSON.parse(reductionFactorData);
        } else {
            reductionFactor = reductionFactorData;
        }
        console.log("Parsed data:", reductionFactor);
    } catch (e) {
        console.error("Error parsing reduction factor data:", e);
        return;
    }

    // Extract values
    const xValues = [0.00, 2.82, 8.48, 12.00];
    const yValues = [0.6, 0.6, 1.0, 1.0];

    // Extract common points
    let commonX, commonY;
    try {
        commonX = parseFloat(reductionFactor.commonX);
        commonY = parseFloat(reductionFactor.commonY);
        console.log("Common point:", commonX, commonY);
    } catch (e) {
        console.error("Error extracting common point:", e);
        commonX = 3.2;  // Use default values if parsing fails
        commonY = 0.85;
    }

    // Create data points
    const lineData = xValues.map((x, i) => {
        return { x: parseFloat(x), y: parseFloat(yValues[i]) };
    });

    // Clean up any existing chart
    if (window.myChart1) {
        window.myChart1.destroy();
    }

    // Create new chart
    window.myChart1 = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Short Voyage Reduction Factor',
                    data: lineData,
                    borderColor: 'blue',
                    borderWidth: 2,
                    fill: false,
                    tension: 0
                },
                {
                    label: 'Hs,max (m)',
                    data: [{ x: commonX, y: commonY }],
                    backgroundColor: 'red',
                    type: 'scatter',
                    pointRadius: 8,
                    pointHoverRadius: 10
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    title: {
                        display: true,
                        text: 'Forecast maximum significant wave height Hs,max(m)'
                    }
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Short Voyage Reduction Factor'
                    },
                    grid: {
                        color: '#e0e0e0'
                    },
                    min: 0,
                    max: 1.2,
                    ticks: {
                        stepSize: 0.2
                    }
                }
            }
        }
    });

    console.log("Chart created!");
};

// graph-2
window.drawChart2 = (canvasid, reductionFactorData) => {
    console.log("Draw chart called for canvas:", canvasid);
    console.log("Received data type:", typeof reductionFactorData);
    console.log("Received data:", reductionFactorData);

    // Make sure Chart is defined
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded!");
        return;
    }

    // Get canvas element
    const canvas = document.getElementById(canvasid);
    if (!canvas) {
        console.error(`Canvas element with id '${canvasid}' not found!`);
        return;
    }

    console.log("Canvas found:", canvas);

    // Get the 2D context
    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("Could not get 2D context from canvas!");
        return;
    }

    // Parse the reduction factor data if it's a string
    let reductionFactor;
    try {
        if (typeof reductionFactorData === 'string') {
            reductionFactor = JSON.parse(reductionFactorData);
        } else {
            reductionFactor = reductionFactorData;
        }
        console.log("Parsed data:", reductionFactor);
    } catch (e) {
        console.error("Error parsing reduction factor data:", e);
        return;
    }

    // Extract values
    const xValues = [0.00, 2.82, 8.48, 12.00];
    const yValues = [0.6, 0.6, 1.0, 1.0];

    // Extract common points
    let commonX, commonY;
    try {
        commonX = parseFloat(reductionFactor.commonX);
        commonY = parseFloat(reductionFactor.commonY);
        console.log("Common point:", commonX, commonY);
    } catch (e) {
        console.error("Error extracting common point:", e);
        commonX = 3.2;  // Use default values if parsing fails
        commonY = 0.85;
    }

    // Create data points
    const lineData = xValues.map((x, i) => {
        return { x: parseFloat(x), y: parseFloat(yValues[i]) };
    });

    // Clean up any existing chart
    if (window.myChart) {
        window.myChart.destroy();
    }

    // Create new chart
    window.myChart = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Short Voyage Reduction Factor',
                    data: lineData,
                    borderColor: 'blue',
                    borderWidth: 2,
                    fill: false,
                    tension: 0
                },
                {
                    label: 'Hs,max (m)',
                    data: [{ x: commonX, y: commonY }],
                    backgroundColor: 'red',
                    type: 'scatter',
                    pointRadius: 8,
                    pointHoverRadius: 10
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    title: {
                        display: true,
                        text: 'Forecast maximum significant wave height Hs,max(m)'
                    }
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Short Voyage Reduction Factor'
                    },
                    grid: {
                        color: '#e0e0e0'
                    },
                    min: 0,
                    max: 1.2,
                    ticks: {
                        stepSize: 0.2
                    }
                }
            }
        }
    });

    console.log("Chart created!");
};


// chart report
// Chart drawing utilities with proper Chart.js integration

// Main chart drawing function (consolidated from your two functions)
window.drawChart = (canvasId, reductionFactorData, chartIndex = 1) => {
    console.log(`Draw chart called for canvas: ${canvasId}, chart index: ${chartIndex}`);
    console.log("Received data type:", typeof reductionFactorData);
    console.log("Received data:", reductionFactorData);

    // Make sure Chart is defined
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded!");
        return false;
    }

    // Get canvas element
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error(`Canvas element with id '${canvasId}' not found!`);
        return false;
    }

    console.log("Canvas found:", canvas);

    // Get the 2D context
    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("Could not get 2D context from canvas!");
        return false;
    }

    // Parse the reduction factor data if it's a string
    let reductionFactor;
    try {
        if (typeof reductionFactorData === 'string') {
            reductionFactor = JSON.parse(reductionFactorData);
        } else {
            reductionFactor = reductionFactorData;
        }
        console.log("Parsed data:", reductionFactor);
    } catch (e) {
        console.error("Error parsing reduction factor data:", e);
        return false;
    }

    // Extract values (default values for Short Voyage Reduction Factor)
    const xValues = [0.00, 2.82, 8.48, 12.00];
    const yValues = [0.6, 0.6, 1.0, 1.0];

    // Extract common points
    let commonX, commonY;
    try {
        commonX = parseFloat(reductionFactor.commonX || reductionFactor.x || 3.2);
        commonY = parseFloat(reductionFactor.commonY || reductionFactor.y || 0.85);
        console.log("Common point:", commonX, commonY);
    } catch (e) {
        console.error("Error extracting common point:", e);
        commonX = 3.2;  // Default values
        commonY = 0.85;
    }

    // Create data points
    const lineData = xValues.map((x, i) => {
        return { x: parseFloat(x), y: parseFloat(yValues[i]) };
    });

    // Clean up any existing chart (use different variable names for multiple charts)
    const chartVariableName = `myChart${chartIndex}`;
    if (window[chartVariableName]) {
        window[chartVariableName].destroy();
    }

    // Create new chart
    window[chartVariableName] = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Short Voyage Reduction Factor',
                    data: lineData,
                    borderColor: 'blue',
                    borderWidth: 2,
                    fill: false,
                    tension: 0,
                    pointRadius: 4,
                    pointHoverRadius: 6
                },
                {
                    label: 'Hs,max (m)',
                    data: [{ x: commonX, y: commonY }],
                    backgroundColor: 'red',
                    borderColor: 'red',
                    type: 'scatter',
                    pointRadius: 8,
                    pointHoverRadius: 10
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                }
            },
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    title: {
                        display: true,
                        text: 'Forecast maximum significant wave height Hs,max(m)',
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: '#e0e0e0'
                    },
                    min: 0,
                    max: 14
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Short Voyage Reduction Factor',
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: '#e0e0e0'
                    },
                    min: 0,
                    max: 1.2,
                    ticks: {
                        stepSize: 0.2
                    }
                }
            }
        }
    });

    console.log(`Chart ${chartIndex} created successfully!`);
    return true;
};

// Specific function for chart 1 (backwards compatibility)
window.drawChart1 = (canvasId, reductionFactorData) => {
    return window.drawChart(canvasId, reductionFactorData, 1);
};

// Specific function for chart 2 (backwards compatibility)
window.drawChart2 = (canvasId, reductionFactorData) => {
    return window.drawChart(canvasId, reductionFactorData, 2);
};

// Get chart as base64 image
window.getChartAsBase64 = (canvasId) => {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found!`);
            return null;
        }

        return canvas.toDataURL('image/png');
    } catch (error) {
        console.error('Error getting chart as base64:', error);
        return null;
    }
};

// Destroy chart by canvas ID
window.destroyChart = (canvasId, chartIndex = 1) => {
    try {
        const chartVariableName = `myChart${chartIndex}`;
        if (window[chartVariableName]) {
            window[chartVariableName].destroy();
            window[chartVariableName] = null;
            console.log(`Chart ${chartIndex} destroyed`);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error destroying chart:', error);
        return false;
    }
};

// Resize chart
window.resizeChart = (canvasId, chartIndex = 1) => {
    try {
        const chartVariableName = `myChart${chartIndex}`;
        if (window[chartVariableName]) {
            window[chartVariableName].resize();
            console.log(`Chart ${chartIndex} resized`);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error resizing chart:', error);
        return false;
    }
};
//add this function for selecting waypoints on map while editing
function editWaypoint(lat, lon) {

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