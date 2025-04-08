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

        // 👉 Temporarily remove route layer from the map
        if (window.routeLayer && mapInstance.hasLayer(window.routeLayer)) {
            mapInstance.removeLayer(window.routeLayer);
        }

        // Delay to ensure layer is removed visually before capture
        await new Promise(resolve => setTimeout(resolve, 100));

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

        const bounds = mapInstance.getBounds();
        const nw = mapInstance.latLngToLayerPoint(bounds.getNorthWest());

        function latLngToCanvasPoint(latLng) {
            const layerPoint = mapInstance.latLngToLayerPoint(latLng);
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
                    if (Array.isArray(points[0])) {
                        points.forEach(segment => {
                            segment.forEach((pt, idx) => {
                                const point = latLngToCanvasPoint(pt);
                                if (idx === 0) ctx.moveTo(point.x, point.y);
                                else ctx.lineTo(point.x, point.y);
                            });
                        });
                    } else {
                        points.forEach((pt, idx) => {
                            const point = latLngToCanvasPoint(pt);
                            if (idx === 0) ctx.moveTo(point.x, point.y);
                            else ctx.lineTo(point.x, point.y);
                        });
                    }

                    ctx.stroke();
                }
            });
        }

        function drawMarker(pin, color) {
            if (pin) {
                const point = latLngToCanvasPoint(pin.getLatLng());
                ctx.beginPath();
                ctx.fillStyle = color;
                ctx.arc(point.x, point.y, 6, 0, 2 * Math.PI);
                ctx.fill();
                ctx.strokeStyle = 'white';
                ctx.lineWidth = 1.5;
                ctx.stroke();
            }
        }

        if (window.departurePin) drawMarker(window.departurePin, 'blue');
        if (window.arrivalPin) drawMarker(window.arrivalPin, 'red');

        // Convert final canvas to image
        const finalImageUrl = canvas.toDataURL();
        window.latestMapImageUrl = finalImageUrl;

        const displayElement = document.getElementById(displayElementId);
        displayElement.innerHTML = '';

        const previewImage = document.createElement('img');
        previewImage.src = finalImageUrl;
        previewImage.style.cursor = 'pointer';
        previewImage.style.maxWidth = '100%';
        previewImage.style.height = 'auto';

        displayElement.appendChild(previewImage);

        // Optional: show full image on click (e.g. in modal)
        previewImage.addEventListener('click', () => {
            const imgWindow = window.open("", "_blank", "width=800,height=600");

            const htmlContent = `
        <html>
        <head>
            <title>Map Preview</title>
            <style>
                body { margin: 0; padding: 0; text-align: center; background: #f5f5f5; }
                .close-btn {
                    position: fixed;
                    top: 10px;
                    right: 15px;
                    font-size: 20px;
                    font-weight: bold;
                    cursor: pointer;
                    background: #fff;
                    padding: 5px 10px;
                    border: 1px solid #ccc;
                    border-radius: 5px;
                    z-index: 1000;
                }
                img {
                    margin-top: 40px;
                    max-width: 90%;
                    max-height: 90vh;
                }
            </style>
        </head>
        <body>
            <div class="close-btn" onclick="window.close()">❌ Close</div>
            <img src="${finalImageUrl}" alt="Map Preview" />
        </body>
        </html>
    `;

            imgWindow.document.write(htmlContent);
        });

        // 👉 Re-add the route layer after capture
        if (window.routeLayer && !mapInstance.hasLayer(window.routeLayer)) {
            mapInstance.addLayer(window.routeLayer);
        }
        return true;

    } catch (error) {
        console.error('Screenshot error:', error);
        console.error(error.stack);
        return false;
    }
};
