var routeLayer = L.layerGroup();
var map;

// Store route segments from FastAPI calls
let routeSegmentsList = [];
// Niranjan modified by - Store route information for popup display
let routeInfo = {};

function initializeRouteListRouteCalculation() {
    // Clear previous route layer efficiently
    routeSegmentsList = [];
    routeLayer.clearLayers();
    // Niranjan modified by - Clear route info
    routeInfo = {};
}

async function searchLocationOnMap(lat = null, lon = null) {
    try {
        // If lat and lon are provided, use them directly
        if (lat !== null && lon !== null) {
            // Convert to float to ensure proper handling
            lat = parseFloat(lat);
            lon = parseFloat(lon);

            // Create new departure pin with optimized marker
            locPin = L.marker([lat, lon], {
                shadowPane: false,
                // Avoid rasterization to improve performance
                bubblingMouseEvents: true
            }).addTo(map);

            //zoomInThenOut(lat, lon);
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Niranjan modified by - Updated function signature to accept additional parameters
function showRouteSegment(routeJson, segmentIndex, totalSegments, reductionFactor = "0", lineColor, routeName = "", departurePortName = "", departurePortUnlocode = "", arrivalPortName = "", arrivalPortUnlocode = "", totalDistance = "0") {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Store this segment
        routeSegmentsList[segmentIndex] = segment;

        // Niranjan modified by - Store route information for the first segment
        if (segmentIndex === 0) {
            routeInfo = {
                routeName: routeName,
                departurePortName: departurePortName,
                departurePortUnlocode: departurePortUnlocode,
                arrivalPortName: arrivalPortName,
                arrivalPortUnlocode: arrivalPortUnlocode,
                reductionFactor: reductionFactor,
                totalDistance: totalDistance
            };
        }

        // If all segments are received, draw the combined route
        if (routeSegmentsList.filter(s => s !== null).length === totalSegments) {
            // Use requestAnimationFrame for smoother rendering
            window.requestAnimationFrame(() => {
                drawRoute(routeSegmentsList, reductionFactor, lineColor);
            });
        }

        return segment;
    } catch (error) {
        console.error("Error processing route segment:", error);
        return null;
    }
}

function drawRoute(segments, reductionFactor, lineColor) {

    if (!segments || segments.length === 0) {
        return;
    }

    // Pre-allocate arrays for better performance
    const allCoordinates = [];
    const segmentBoundaries = [];
    // Niranjan modified by - Removed totalDistance and totalDuration calculation as it's passed from C#

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

            // Niranjan modified by - Removed distance and duration calculation
        }
    });

    // Create route polyline with optimized options
    const routePolyline = L.polyline(allCoordinates, {
        color: lineColor,
        weight: 3,
        opacity: 0.8,
        smoothFactor: 1,
        // Improve performance by reducing points on zoom
        interactive: false
    }).addTo(routeLayer);


    if (allCoordinates.length > 0) {
        const midIndex = Math.floor(allCoordinates.length / 2);
        const midPoint = allCoordinates[midIndex];

        // Niranjan modified by - Updated popup content with route information from C#
        const distanceMarkerHtml = `<div style="background-color: white; padding: 8px 12px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold; min-width: 200px;">
        <div style="margin-bottom: 4px;"><strong>Route name:</strong> ${routeInfo.routeName || 'N/A'}</div>
        <div style="margin-bottom: 4px;"><strong>${routeInfo.departurePortUnlocode || 'N/A'}</strong> - <strong>${routeInfo.arrivalPortUnlocode || 'N/A'}</strong></div>
        <div style="margin-bottom: 4px;"><strong>Distance:</strong> ${routeInfo.totalDistance || '0'} nm</div>
        <div><strong>Reduction factor:</strong> ${routeInfo.reductionFactor || '0'}</div>
        </div>`;

        L.marker(midPoint, {
            icon: L.divIcon({
                className: 'distance-marker',
                html: distanceMarkerHtml,
                iconSize: null
            })
        }).addTo(routeLayer);
    }

    updatePinsToMatchAllRoutePointsList(allCoordinates, segmentBoundaries);

    const routeBounds = L.latLngBounds(allCoordinates);

    map.fitBounds(routeBounds, {
        padding: [50, 50]
    });

    routeSegmentsList = [];
    return routePolyline;
}

function updatePinsToMatchAllRoutePointsList(allCoordinates, segmentBoundaries) {
    if (window.departurePin) { map.removeLayer(window.departurePin); window.departurePin = null; }
    if (window.arrivalPin) { map.removeLayer(window.arrivalPin); window.arrivalPin = null; }
    if (window.portPins && Array.isArray(window.portPins)) {
        window.portPins.forEach(pin => map.removeLayer(pin));
    }
    window.portPins = [];
    if (window.waypointPins && Array.isArray(window.waypointPins)) {
        window.waypointPins.forEach(pin => map.removeLayer(pin));
    }
    window.waypointPins = [];

    for (let i = 0; i < segmentBoundaries.length; i++) {
        let coordIdx;
        let label = '';
        if (i === 0) {
            coordIdx = segmentBoundaries[0].startIndex;
            label = 'Departure';
        } else {
            coordIdx = segmentBoundaries[i - 1].endIndex;
            label = i === segmentBoundaries.length - 1 ? 'Arrival' : `Port/WP ${i}`;
        }
        if (coordIdx < 0 || coordIdx >= allCoordinates.length) continue;
        let pin = L.marker(allCoordinates[coordIdx]).addTo(map);
        if (i === 0) {
            window.departurePin = pin;
            pin.bindPopup(label);
        } else if (i === segmentBoundaries.length - 1) {
            window.arrivalPin = pin;
            pin.bindPopup(label);
        } else {
            pin.bindPopup(label);
            window.portPins.push(pin);
        }
    }
}

function zoomOutMap() {
    const bounds = map.getBounds();
    map.fitBounds(bounds, {
        padding: [50, 50]
    });
    map.setZoom(2);
}