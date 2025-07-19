var routeLayer = L.layerGroup();
var map;

// Store route segments from FastAPI calls
let routeSegmentsList = [];
let routeInfoList = []; // Store route information for each segment

function initializeRouteListRouteCalculation() {
    // Clear previous route layer efficiently
    routeSegmentsList = [];
    routeInfoList = [];
    routeLayer.clearLayers();
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

function showRouteSegment(routeJson, segmentIndex, totalSegments, reductionFactor = "0", lineColor,
    routeName = "", departurePortName = "", departurePortUnlocode = "",
    arrivalPortName = "", arrivalPortUnlocode = "", totalDistance = "0") {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Store this segment
        routeSegmentsList[segmentIndex] = segment;

        // Store route information for this segment
        routeInfoList[segmentIndex] = {
            routeName: routeName,
            departurePortName: departurePortName,
            departurePortUnlocode: departurePortUnlocode,
            arrivalPortName: arrivalPortName,
            arrivalPortUnlocode: arrivalPortUnlocode,
            totalDistance: totalDistance,
            reductionFactor: reductionFactor,
            lineColor: lineColor
        };

        // If all segments are received, draw the combined route
        if (routeSegmentsList.filter(s => s !== null).length === totalSegments) {
            // Use requestAnimationFrame for smoother rendering
            window.requestAnimationFrame(() => {
                drawRoute(routeSegmentsList, segmentIndex, lineColor);
            });
        }

        return segment;
    } catch (error) {
        console.error("Error processing route segment:", error);
        return null;
    }
}

function drawRoute(segments, currentSegmentIndex, lineColor) {
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

        // Get route information from the current segment
        const routeInfo = routeInfoList[currentSegmentIndex] || {};

        // Create enhanced popup content with all required information
        const distanceMarkerHtml = `<div style="background-color: white; padding: 8px 12px; border-radius: 8px; border: 2px solid ${lineColor}; font-weight: bold; min-width: 250px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);">
            <div style="color: ${lineColor}; font-size: 14px; margin-bottom: 8px; border-bottom: 1px solid #eee; padding-bottom: 4px;">
                <strong>Route: ${routeInfo.routeName || 'Unknown Route'}</strong>
            </div>
            <div style="font-size: 12px; line-height: 1.4;">
                <div style="margin-bottom: 4px;">
                    <strong>Departure:</strong> ${routeInfo.departurePortUnlocode || 'N/A'} - ${routeInfo.departurePortName || 'Unknown'}
                </div>
                <div style="margin-bottom: 4px;">
                    <strong>Arrival:</strong> ${routeInfo.arrivalPortUnlocode || 'N/A'} - ${routeInfo.arrivalPortName || 'Unknown'}
                </div>
                <div style="margin-bottom: 4px;">
                    <strong>Distance:</strong> ${routeInfo.totalDistance || totalDistance} nm
                </div>
                <div>
                    <strong>Reduction Factor:</strong> ${routeInfo.reductionFactor || '0'}
                </div>
            </div>
        </div>`;

        L.marker(midPoint, {
            icon: L.divIcon({
                className: 'enhanced-distance-marker',
                html: distanceMarkerHtml,
                iconSize: null,
                iconAnchor: [125, 60] // Center the popup
            })
        }).addTo(routeLayer);
    }

    updatePinsToMatchAllRoutePointsList(allCoordinates, segmentBoundaries);

    const routeBounds = L.latLngBounds(allCoordinates);

    map.fitBounds(routeBounds, {
        padding: [50, 50]
    });

    routeSegmentsList = [];
    routeInfoList = [];
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

// Add some CSS for the enhanced popup
if (!document.getElementById('route-popup-styles')) {
    const style = document.createElement('style');
    style.id = 'route-popup-styles';
    style.textContent = `
        .enhanced-distance-marker {
            background: transparent !important;
            border: none !important;
        }
        
        .enhanced-distance-marker div {
            animation: fadeIn 0.3s ease-in;
        }
        
        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(-10px); }
            to { opacity: 1; transform: translateY(0); }
        }
    `;
    document.head.appendChild(style);
}