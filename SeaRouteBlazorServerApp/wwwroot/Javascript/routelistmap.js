var routeLayer = L.layerGroup();
var map;

// Store route segments from FastAPI calls
let routeSegmentsList = [];
function initializeRouteListRouteCalculation() {
    // Clear previous route layer efficiently
    routeSegmentsList = [];
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

function showRouteSegment(routeJson, segmentIndex, totalSegments, reductionFactor = "0", lineColor) {
    try {
        const segment = createSeaRoutefromAPI(routeJson);

        // Store this segment
        routeSegmentsList[segmentIndex] = segment;
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

    // Add total distance and duration marker only when needed
    if (allCoordinates.length > 0) {
        const midIndex = Math.floor(allCoordinates.length / 2);
        const midPoint = allCoordinates[midIndex];
        // Create marker with precomputed HTML for better performance
        const distanceMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
        Distance: ${totalDistance} nm<br>
        Reduction Factor: ${reductionFactor}
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
    //if (segmentBoundaries.length < 10) {
    //    segmentBoundaries.forEach((boundary, idx) => {
    //        const segmentMidIndex = Math.floor((boundary.startIndex + boundary.endIndex) / 2);
    //        const segmentMidPoint = allCoordinates[segmentMidIndex];

    //        const segmentProps = boundary.properties;
    //        const segmentDistance = segmentProps && segmentProps.length ? Math.round(segmentProps.length) : 0;
    //        const segmentDuration = segmentProps && segmentProps.duration_hours ? Math.round(segmentProps.duration_hours) : 0;

    //        const fromPoint = routePoints[idx];
    //        const toPoint = routePoints[idx + 1];
    //        const fromName = fromPoint ? fromPoint.name || 'Point' : 'Point';
    //        const toName = toPoint ? toPoint.name || 'Point' : 'Point';

    //        // Segment marker with precomputed HTML
    //        const segmentMarkerHtml = `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">
    //            Distance: ${segmentDistance} nm<br>
    //            ReductionFactor: ${reductionFactor}
    //        </div>`;

    //        const segmentMarker = L.marker(segmentMidPoint, {
    //            icon: L.divIcon({
    //                className: 'distance-marker',
    //                html: segmentMarkerHtml,
    //                iconSize: null
    //            })
    //        }).addTo(routeLayer);

    //        // Use efficient tooltip creation
    //        segmentMarker.bindTooltip(`${fromName} → ${toName}
    //            {segmentDistance} nm / ${segmentDuration} hours`,
    //            { permanent: false, direction: 'top', offset: [0, -10] });
    //    });
    //}

     // Place pins for all route points (departure, intermediates, arrival) after normalization
     updatePinsToMatchAllRoutePointsList(allCoordinates, segmentBoundaries);
    //Calculate bounds more efficiently
    const routeBounds = L.latLngBounds(allCoordinates);
    /*const paddingLeft = window.innerWidth * 0.45;*/

    //Use an optimized flyToBounds that's less demanding
    map.fitBounds(routeBounds, {
        //paddingTopLeft: [paddingLeft, 20],
        //paddingBottomRight: [20, 20]
        padding: [50, 50]
    });

    // Reset routeSegmentsList after rendering
    routeSegmentsList = [];
    return routePolyline;
}

// Place pins at start/end/intermediate points after normalization (like map.js)
function updatePinsToMatchAllRoutePointsList(allCoordinates, segmentBoundaries) {
    // Remove all existing pins (if you have global pin variables, clear them here)
    if (window.routeListPins) {
        window.routeListPins.forEach(pin => map.removeLayer(pin));
    }
    window.routeListPins = [];

    // Place pins at the start of the first segment, end of each segment
    for (let i = 0; i < segmentBoundaries.length; i++) {
        let coordIdx;
        let label = '';
        if (i === 0) {
            // First point: start of first segment
            coordIdx = segmentBoundaries[0].startIndex;
            label = 'Departure';
        } else {
            // All others: end of previous segment
            coordIdx = segmentBoundaries[i - 1].endIndex;
            label = i === segmentBoundaries.length - 1 ? 'Arrival' : `Port/WP ${i}`;
        }
        if (coordIdx < 0 || coordIdx >= allCoordinates.length) continue;
        let pin = L.marker(allCoordinates[coordIdx]).addTo(map);
        pin.bindPopup(label);
        window.routeListPins.push(pin);
    }
}

function zoomOutMap() {
    const bounds = map.getBounds();
    map.fitBounds(bounds, {
        //paddingTopLeft: [20, 20],
        //paddingBottomRight: [20, 20],
        padding: [50, 50]
    });
    map.setZoom(2);
}