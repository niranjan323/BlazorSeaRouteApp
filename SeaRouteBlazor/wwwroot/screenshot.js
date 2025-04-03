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



window.captureAndDisplayMap = async (mapElementId, displayElementId) => {
    try {
        const mapElement = document.getElementById(mapElementId);

        // Hide overlays temporarily
        const overlays = mapElement.querySelectorAll('.map-overlay');
        overlays.forEach(overlay => overlay.style.display = 'none');

        // Capture screenshot
        const canvas = await html2canvas(mapElement, {
            logging: false,
            useCORS: true,
            scale: 1
        });

        // Restore overlays
        overlays.forEach(overlay => overlay.style.display = '');

        // Display the result
        const displayElement = document.getElementById(displayElementId);
        displayElement.innerHTML = '';
        displayElement.appendChild(canvas);

        return true;
    } catch (error) {
        console.error('Screenshot error:', error);
        return false;
    }
};