var map;
var departurePin;
var arrivalPin;
var routeLayer;
var isWaypointSelectionActive = false;
var coordTooltip;
var currentDotNetHelper;

function initializeMap(dotNetHelper) {
    currentDotNetHelper = dotNetHelper;
    map = L.map('map').setView([20, 60], 3);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // Add zoom controls
    L.control.zoom({
        position: 'topright'
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

            // Optionally disable selection after click
            setWaypointSelection(false);
        }
    });
}

function setWaypointSelection(active) {
    isWaypointSelectionActive = active;
    if (!active) {
        coordTooltip.style.display = 'none';
    }
    else {
        // Change cursor to crosshair when in selection mode
        map.getContainer().style.cursor = 'crosshair';
    }
}

// Rest of your existing functions (searchLocation, etc.) remain unchanged


//var map;
//var departurePin;
//var arrivalPin;
//var routeLayer;


//function initializeMap(dotNetHelper) {
//    map = L.map('map').setView([20, 60], 3);
//    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
//        attribution: '© OpenStreetMap contributors'
//    }).addTo(map);

//    // Add zoom controls
//    L.control.zoom({
//        position: 'topright'
//    }).addTo(map);

//    // Initialize the route layer
//    routeLayer = L.layerGroup().addTo(map);

//    // Create coordinate tooltip
//    let coordTooltip = L.DomUtil.create('div', 'coord-tooltip');
//    coordTooltip.style.position = 'absolute';
//    coordTooltip.style.pointerEvents = 'none';
//    coordTooltip.style.backgroundColor = 'rgba(255, 255, 255, 0.8)';
//    coordTooltip.style.padding = '3px 6px';
//    coordTooltip.style.borderRadius = '3px';
//    coordTooltip.style.zIndex = '1000';
//    coordTooltip.style.display = 'none';
//    map.getContainer().appendChild(coordTooltip);

//    // Show coordinates on mouse move
//    map.on('mousemove', function (e) {
//        const { lat, lng } = e.latlng;
//        coordTooltip.textContent = `Lat: ${lat.toFixed(4)}, Lng: ${lng.toFixed(4)}`;
//        coordTooltip.style.display = 'block';
//        coordTooltip.style.left = (e.originalEvent.clientX + 10) + 'px';
//        coordTooltip.style.top = (e.originalEvent.clientY + 10) + 'px';
//    });

//    // Hide coordinates when mouse leaves map
//    map.getContainer().addEventListener('mouseleave', function () {
//        coordTooltip.style.display = 'none';
//    });

//    map.on('click', function (e) {
//        var latitude = e.latlng.lat;
//        var longitude = e.latlng.lng;

//        console.log("Captured Coordinates:", latitude, longitude);

//        // Call Blazor method
//        dotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);
//    });
//}


async function searchLocation(query, isDeparture) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();

        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);
            map.flyTo([lat + 5, lon + 5], 3, { duration: 1.5 });
            // Clear previous pin
            if (isDeparture) {
                if (departurePin) {
                    map.removeLayer(departurePin);
                }
                departurePin = L.marker([lat, lon]).addTo(map);
                departurePin.bindPopup("Departure: " + query).openPopup();
            } else {
                if (arrivalPin) {
                    map.removeLayer(arrivalPin);
                }
                arrivalPin = L.marker([lat, lon]).addTo(map);
                arrivalPin.bindPopup("Arrival: " + query).openPopup();
            }
            setTimeout(() => {

                map.flyTo([lat, lon], 3, { duration: 1.5 });
               
            }, 2000);

            map.flyTo([lat, lon], 8, { duration: 1.5 });
            
            if (departurePin && arrivalPin) {
                drawSeaRoute();
            }
        } else {
            alert("Location not found!");
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

function drawSeaRoute() {
    // Clear previous route
    routeLayer.clearLayers();

    const departureLatLng = departurePin.getLatLng();
    const arrivalLatLng = arrivalPin.getLatLng();

    // This is a simplified sea route - in a real app you'd use a proper routing API
    const routeCoordinates = [
        departureLatLng,
        [departureLatLng.lat, (departureLatLng.lng + arrivalLatLng.lng) / 2],
        [arrivalLatLng.lat, (departureLatLng.lng + arrivalLatLng.lng) / 2],
        arrivalLatLng
    ];

    // Draw the route
    L.polyline(routeCoordinates, {
        color: 'blue',
        weight: 2,
        opacity: 1
    }).addTo(routeLayer);

    // Add distance marker
    const distance = calculateDistance(departureLatLng, arrivalLatLng);
    const midpoint = routeCoordinates[Math.floor(routeCoordinates.length / 2)];
    L.marker(midpoint, {
        icon: L.divIcon({
            className: 'distance-marker',
            html: `<div style="background-color: white; padding: 2px 5px; border-radius: 3px; border: 1px solid #0066ff;">${distance} km</div>`,
            iconSize: null
        })
    }).addTo(routeLayer);
}

function calculateDistance(latlng1, latlng2) {
    const R = 6371; // Radius of the earth in km
    const dLat = (latlng2.lat - latlng1.lat) * Math.PI / 180;
    const dLon = (latlng2.lng - latlng1.lng) * Math.PI / 180;
    const a =
        Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(latlng1.lat * Math.PI / 180) * Math.cos(latlng2.lat * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    const distance = R * c;
    return Math.round(distance);
}




// this for chat 
let chartInstance = null;

window.createChart = (canvasId, config) => {
    console.log("Received JSON string:", config);

    try {
        const parsedConfig = typeof config === 'string' ? JSON.parse(config) : config;
        console.log("Parsed JSON:", parsedConfig);

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas with ID '${canvasId}' not found.`);
            return;
        }

        // Destroy previous instance if it exists
        if (window.chartInstance) {
            window.chartInstance.destroy();
        }

        // Create new chart
        window.chartInstance = new Chart(canvas, parsedConfig);
        console.log("Chart created successfully!");
    } catch (error) {
        console.error("Error parsing JSON:", error);
    }
};

// this for report
let reportInstance = null;

window.createReport = (canvasId, config) => {
    console.log("Received JSON string:", config);

    try {
        const parsedConfig = typeof config === 'string' ? JSON.parse(config) : config;
        console.log("Parsed JSON:", parsedConfig);

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas with ID '${canvasId}' not found.`);
            return;
        }

        // Destroy previous instance if it exists
        if (window.reportInstance) {
            window.reportInstance.destroy();
        }

        // Create new chart
        window.chartInstance = new Chart(canvas, parsedConfig);
        console.log("Chart created successfully!");
    } catch (error) {
        console.error("Error parsing JSON:", error);
    }
};
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

    // Reset waypoint selection
    isWaypointSelectionActive = false;
    map.getContainer().style.cursor = '';

    // Reset the map view to the initial state
    map.setView([20, 60], 3);
}



