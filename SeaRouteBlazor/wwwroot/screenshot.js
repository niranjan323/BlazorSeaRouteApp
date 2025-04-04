//function captureLeafletMap(mapId, displayElementId) {
//    try {
//        var map = window[mapId];  // Ensure the map instance exists
//        if (!map) {
//            console.error("Map instance not found:", mapId);
//            return false;
//        }

//        leafletImage(map, function (err, canvas) {
//            if (err) {
//                console.error('Error capturing map:', err);
//                return false;
//            }
//            const displayElement = document.getElementById(displayElementId);
//            if (displayElement) {
//                displayElement.innerHTML = '';
//                displayElement.appendChild(canvas);
//            }
//        });
//        return true;
//    } catch (e) {
//        console.error('Exception in captureLeafletMap:', e);
//        return false;
//    }
//}



//window.captureAndDisplayMap = async (mapElementId, displayElementId) => {
//    try {
//        const mapElement = document.getElementById(mapElementId);

//        // Hide overlays temporarily
//        const overlays = mapElement.querySelectorAll('.map-overlay');
//        overlays.forEach(overlay => overlay.style.display = 'none');

//        // Capture screenshot
//        const canvas = await html2canvas(mapElement, {
//            logging: false,
//            useCORS: true,
//            scale: 1
//        });

//        // Restore overlays
//        overlays.forEach(overlay => overlay.style.display = '');

//        // Display the result
//        const displayElement = document.getElementById(displayElementId);
//        displayElement.innerHTML = '';
//        displayElement.appendChild(canvas);

//        return true;
//    } catch (error) {
//        console.error('Screenshot error:', error);
//        return false;
//    }
//};


window.captureMapWithRoutes = async (mapElementId, displayElementId) => {
    try {
        const mapElement = document.getElementById(mapElementId);
        const mapInstance = window.map;

        if (!mapInstance) {
            console.error("Map instance not found");
            return false;
        }

        // First, create a base screenshot using html2canvas
        const baseCanvas = await html2canvas(mapElement, {
            useCORS: true,
            allowTaint: true,
            backgroundColor: null,
            scale: 1
        });

        // Create our final canvas with the same dimensions
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        canvas.width = baseCanvas.width;
        canvas.height = baseCanvas.height;

        // Draw the base map
        ctx.drawImage(baseCanvas, 0, 0);

        // Save original map bounds and zoom level to calculate coordinates correctly
        const bounds = mapInstance.getBounds();
        const nw = mapInstance.latLngToLayerPoint(bounds.getNorthWest());
        const se = mapInstance.latLngToLayerPoint(bounds.getSouthEast());

        // Helper function to convert lat/lng to pixel coordinates on our canvas
        function latLngToCanvasPoint(latLng) {
            const layerPoint = mapInstance.latLngToLayerPoint(latLng);

            // Calculate the position on our canvas using the bounds
            const x = layerPoint.x - nw.x;
            const y = layerPoint.y - nw.y;

            return { x, y };
        }

        // Draw the route
        if (window.routeLayer) {
            window.routeLayer.eachLayer(layer => {
                if (layer instanceof L.Polyline) {
                    ctx.beginPath();
                    ctx.strokeStyle = layer.options.color || 'blue';
                    ctx.lineWidth = layer.options.weight || 2;

                    const points = layer.getLatLngs();
                    for (let i = 0; i < points.length; i++) {
                        // If points is a nested array (multipolyline), handle it accordingly
                        if (Array.isArray(points[0])) {
                            for (let j = 0; j < points.length; j++) {
                                const segment = points[j];
                                for (let k = 0; k < segment.length; k++) {
                                    const point = latLngToCanvasPoint(segment[k]);
                                    if (k === 0) {
                                        ctx.moveTo(point.x, point.y);
                                    } else {
                                        ctx.lineTo(point.x, point.y);
                                    }
                                }
                            }
                        } else {
                            const point = latLngToCanvasPoint(points[i]);
                            if (i === 0) {
                                ctx.moveTo(point.x, point.y);
                            } else {
                                ctx.lineTo(point.x, point.y);
                            }
                        }
                    }
                    ctx.stroke();
                }
            });
        }

        // Draw markers
        function drawMarker(pin, color) {
            if (pin) {
                const point = latLngToCanvasPoint(pin.getLatLng());

                // Draw pin marker
                ctx.beginPath();
                ctx.fillStyle = color;
                // Draw a map pin shape
                ctx.arc(point.x, point.y, 6, 0, 2 * Math.PI);
                ctx.fill();

                // Add a border
                ctx.strokeStyle = 'white';
                ctx.lineWidth = 1.5;
                ctx.stroke();
            }
        }

        // Draw departure and arrival pins
        if (window.departurePin) {
            drawMarker(window.departurePin, 'blue');
        }

        if (window.arrivalPin) {
            drawMarker(window.arrivalPin, 'red');
        }

        // Display the result
        const displayElement = document.getElementById(displayElementId);
        displayElement.innerHTML = '';
        displayElement.appendChild(canvas);

        // Add download button
        const downloadLink = document.createElement('a');
        downloadLink.href = canvas.toDataURL('image/png');
        downloadLink.download = 'map-with-route.png';
        downloadLink.textContent = 'Download Map';
        downloadLink.className = 'btn btn-primary mt-2';
        displayElement.appendChild(document.createElement('br'));
        displayElement.appendChild(downloadLink);

        return true;
    } catch (error) {
        console.error('Screenshot error:', error);
        console.error(error.stack);
        return false;
    }
};