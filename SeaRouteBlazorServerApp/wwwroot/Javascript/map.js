//var map;
//var departurePin;
//var arrivalPin;
//var routeLayer;
//var isWaypointSelectionActive = false;
//var coordTooltip;
//var currentDotNetHelper;
//let clickedPin = null;
//let newportPin = null;
//let waypoint = null;

//function initializeMap(dotNetHelper) {
//    currentDotNetHelper = dotNetHelper;
//    map = L.map('map').setView([20, 60], 3);
//    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
//        attribution: '© OpenStreetMap contributors', crossOrigin: 'anonymous'
//    }).addTo(map);

//    // Initialize the route layer
//    routeLayer = L.layerGroup().addTo(map);

//    // Create coordinate tooltip
//    coordTooltip = L.DomUtil.create('div', 'coord-tooltip');
//    coordTooltip.style.position = 'absolute';
//    coordTooltip.style.pointerEvents = 'none';
//    coordTooltip.style.backgroundColor = 'rgba(255, 255, 255, 0.9)';
//    coordTooltip.style.padding = '5px 10px';
//    coordTooltip.style.borderRadius = '3px';
//    coordTooltip.style.zIndex = '1000';
//    coordTooltip.style.display = 'none';
//    coordTooltip.style.fontFamily = 'Arial, sans-serif';
//    coordTooltip.style.fontSize = '12px';
//    coordTooltip.style.border = '1px solid #ccc';
//    map.getContainer().appendChild(coordTooltip);

//    // Mouse move handler
//    map.on('mousemove', function (e) {
//        if (isWaypointSelectionActive) {
//            const { lat, lng } = e.latlng;
//            coordTooltip.textContent = `Click to set waypoint\nLat: ${lat.toFixed(4)}, Lng: ${lng.toFixed(4)}`;
//            coordTooltip.style.display = 'block';
//            coordTooltip.style.left = (e.originalEvent.clientX + 15) + 'px';
//            coordTooltip.style.top = (e.originalEvent.clientY + 15) + 'px';
//        }
//    });

//    map.getContainer().addEventListener('mouseleave', function () {
//        coordTooltip.style.display = 'none';
//    });

//    map.on('click', function (e) {
//        if (isWaypointSelectionActive) {
//            var latitude = e.latlng.lat;
//            var longitude = e.latlng.lng;

//            // Call Blazor method
//            currentDotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);

//            // Remove existing pin if any
//            if (clickedPin) {
//                map.removeLayer(clickedPin);
//            }

//            // Add new pin at the clicked location
//            clickedPin = L.marker([latitude, longitude]).addTo(map);
//            clickedPin.bindPopup(`Waypoint: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`).openPopup();

//            // Optionally disable selection after click
//            setWaypointSelection(false);
//        }
//    });

//}

//function setWaypointSelection(active) {
//    isWaypointSelectionActive = active;
//    if (!active) {
//        coordTooltip.style.display = 'none';
//    }
//    else {
//        // Change cursor to crosshair when in selection mode
//        map.getContainer().style.cursor = 'crosshair';
//    }
//}


//async function searchLocation(query, isDeparture) {
//    try {
//        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
//        let data = await response.json();

//        if (data.length > 0) {
//            let lat = parseFloat(data[0].lat);
//            let lon = parseFloat(data[0].lon);

//            // Initial zoom out
//            map.flyTo([lat + 5, lon + 5], 3, { duration: 1.5 });

//            // Clear previous pin
//            if (isDeparture) {
//                if (departurePin) {
//                    map.removeLayer(departurePin);
//                }
//                departurePin = L.marker([lat, lon]).addTo(map);
//                departurePin.bindPopup("Departure: " + query).openPopup();
//            } else {
//                if (arrivalPin) {
//                    map.removeLayer(arrivalPin);
//                }
//                arrivalPin = L.marker([lat, lon]).addTo(map);
//                arrivalPin.bindPopup("Arrival: " + query).openPopup();
//            }
//            if (isDeparture) {
//                setTimeout(() => {
//                    map.flyTo([lat, lon], 3, {
//                        duration: 1.5,
//                        paddingTopLeft: [window.innerWidth * 0.45, 20]
//                    });

//                }, 2000);
//            }


//            // Final zoom in with right shift
//            map.flyTo([lat, lon], 8, {
//                duration: 1.5,
//                paddingTopLeft: [window.innerWidth * 0.45, 20]
//            });

//            if (departurePin && arrivalPin) {
//                drawSeaRoute();
//            }
//        } else {
//            alert("Location not found!");
//        }
//    } catch (error) {
//        console.error("Error fetching location:", error);
//    }
//}

//async function zoomAndPinLocation(query, isDeparture) {
//    try {
//        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
//        let data = await response.json();

//        if (data.length > 0) {
//            let lat = parseFloat(data[0].lat);
//            let lon = parseFloat(data[0].lon);


//            setTimeout(() => {
//                // Add pin
//                if (newportPin) {
//                    map.removeLayer(newportPin);
//                }
//                newportPin = L.marker([lat, lon]).addTo(map);
//                newportPin.bindPopup(`${isDeparture ? "new port" : "Arrival"}: ${query}`).openPopup();
//            }, 2000);
//            setTimeout(() => {

//                map.flyTo([lat, lon], 3, { duration: 1.5 });

//            }, 2000);

//            map.flyTo([lat, lon], 8, { duration: 1.5 });
//        } else {
//            alert("Location not found!");
//        }
//    } catch (error) {
//        console.error("Error fetching location:", error);
//    }
//}

//function drawSeaRoute() {
//    // Clear previous route
//    routeLayer.clearLayers();

//    const departureLatLng = departurePin.getLatLng();
//    const arrivalLatLng = arrivalPin.getLatLng();

//    // Create route coordinates
//    const routeCoordinates = [
//        departureLatLng,
//        [departureLatLng.lat, (departureLatLng.lng + arrivalLatLng.lng) / 2],
//        [arrivalLatLng.lat, (departureLatLng.lng + arrivalLatLng.lng) / 2],
//        arrivalLatLng
//    ];

//    // Draw the route
//    L.polyline(routeCoordinates, {
//        color: 'blue',
//        weight: 2,
//        opacity: 1
//    }).addTo(routeLayer);

//    // Add distance marker
//    const distance = calculateDistance(departureLatLng, arrivalLatLng);
//    const midpoint = routeCoordinates[Math.floor(routeCoordinates.length / 2)];
//    L.marker(midpoint, {
//        icon: L.divIcon({
//            className: 'distance-marker',
//            html: `<div style="background-color: white; padding: 2px 5px; border-radius: 3px; border: 1px solid #0066ff;">${distance} km</div>`,
//            iconSize: null
//        })
//    }).addTo(routeLayer);

//    // Calculate bounds with right padding to account for 40% overlay
//    const routeBounds = L.latLngBounds(routeCoordinates);

//    // Calculate pixel padding - approximately 40% of map width to the left
//    // (since your overlay is 40% width and starts at 25% from left)
//    const paddingLeft = window.innerWidth * 0.45; // Slightly more than overlay width

//    map.flyToBounds(routeBounds, {
//        paddingTopLeft: [paddingLeft, 20],    // Left padding to shift right
//        paddingBottomRight: [20, 20],         // Regular padding on other sides
//        duration: 1
//    });
//}


//function calculateDistance(latlng1, latlng2) {
//    const R = 6371; // Radius of the earth in km
//    const dLat = (latlng2.lat - latlng1.lat) * Math.PI / 180;
//    const dLon = (latlng2.lng - latlng1.lng) * Math.PI / 180;
//    const a =
//        Math.sin(dLat / 2) * Math.sin(dLat / 2) +
//        Math.cos(latlng1.lat * Math.PI / 180) * Math.cos(latlng2.lat * Math.PI / 180) *
//        Math.sin(dLon / 2) * Math.sin(dLon / 2);
//    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
//    const distance = R * c;
//    return Math.round(distance);
//}


//// this for report
//let reportInstance = null;

//window.createReport = (canvasId, config) => {
//    console.log("Received JSON string:", config);

//    try {
//        const parsedConfig = typeof config === 'string' ? JSON.parse(config) : config;
//        console.log("Parsed JSON:", parsedConfig);

//        const canvas = document.getElementById(canvasId);
//        if (!canvas) {
//            console.error(`Canvas with ID '${canvasId}' not found.`);
//            return;
//        }

//        // Destroy previous instance if it exists
//        if (window.reportInstance) {
//            window.reportInstance.destroy();
//        }

//        // Create new chart
//        window.chartInstance = new Chart(canvas, parsedConfig);
//        console.log("Chart created successfully!");
//    } catch (error) {
//        console.error("Error parsing JSON:", error);
//    }
//};
//function resetMap() {
//    // Clear departure and arrival pins
//    if (departurePin) {
//        map.removeLayer(departurePin);
//        departurePin = null;
//    }
//    if (arrivalPin) {
//        map.removeLayer(arrivalPin);
//        arrivalPin = null;
//    }

//    // Clear route layer
//    if (routeLayer) {
//        routeLayer.clearLayers();
//    }
//    if(clickedPin) {
//        map.removeLayer(clickedPin);
//        clickedPin = null;
//    }
//    if(newportPin) {
//        map.removeLayer(newportPin);
//        clickedPin = null;
//    }
//    if (waypoint) {
//        map.removeLayer(waypoint);
//        waypoint = null;
//    }
//    // Reset waypoint selection
//    isWaypointSelectionActive = false;
//    map.getContainer().style.cursor = '';

//    // Reset the map view to the initial state
//    map.setView([20, 60], 3);
//}


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

//function updateMap(name, latitude, longitude) {
//    if (!latitude || !longitude) return;

//    let lat = parseFloat(latitude);
//    let lon = parseFloat(longitude);

//    if (isNaN(lat) || isNaN(lon)) return;

//     waypoint = L.marker([lat, lon]).addTo(map);
//    waypoint.bindPopup(`${name}: ${lat.toFixed(5)}, ${lon.toFixed(5)}`).openPopup();

//    // Adjust map view
//    map.flyTo([lat, lon], 8, { duration: 1.5 });
//}



//-------------
var map;
var departurePin;
var arrivalPin;
var routeLayer;
var isWaypointSelectionActive = false;
var coordTooltip;
var currentDotNetHelper;

// Arrays to store different types of pins
let clickedPins = [];
let portPins = []; // Renamed from newportPins to better reflect their purpose
let waypointPins = [];

// Array to store all route points in sequence
let routePoints = [];

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

            // Reorganize and draw route if we have departure and arrival
            if (departurePin && arrivalPin) {
                reorganizeRoutePoints();
                drawSeaRoute();
            }

            // Optionally disable selection after click
            setWaypointSelection(false);
        }
    });

    // Update the drawSeaRoute function to use solid blue lines
    function drawSeaRoute() {
        // Clear previous route
        routeLayer.clearLayers();

        if (routePoints.length < 2) {
            return; // Need at least departure and arrival to draw a route
        }

        // Collect all lat/lng coordinates in order
        const coordinates = routePoints.map(point => point.latLng);

        // Create a sea route through all points
        const seaRoute = createSeaRoute(coordinates);

        // Draw the route line with solid blue style (no dashes)
        L.polyline(seaRoute, {
            color: '#0066ff',
            weight: 3,
            opacity: 0.8,
            smoothFactor: 1,
            dashArray: null,  // Ensure no dash array is set
            className: 'sea-route-line'
        }).addTo(routeLayer);

        // Add distance markers between segments
        addDistanceMarkers(seaRoute);

        // Note: We're not changing the map view here to avoid interfering with zoomInThenOut
    }



    // Enhanced reorganizeRoutePoints function to handle waypoints better
    function reorganizeRoutePoints() {
        // Extract departure and arrival points
        const departurePoint = routePoints.find(p => p.type === 'departure');
        const arrivalPoint = routePoints.find(p => p.type === 'arrival');

        // Get all intermediate points (ports and waypoints)
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

        // Optional: Sort intermediate points by some logic if needed
        // For example, you might want to sort ports and waypoints by their proximity to the route
    }
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

// Main function to search for a port location and set as departure or arrival
async function searchLocation(query, isDeparture) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();

        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);

            // Clear previous pin
            if (isDeparture) {
                if (departurePin) {
                    map.removeLayer(departurePin);
                }
                departurePin = L.marker([lat, lon]).addTo(map);
                departurePin.bindPopup("Departure: " + query).openPopup();

                // Update route points
                if (routePoints.length === 0) {
                    routePoints.push({
                        type: 'departure',
                        latLng: [lat, lon],
                        name: query
                    });
                } else {
                    routePoints[0] = {
                        type: 'departure',
                        latLng: [lat, lon],
                        name: query
                    };
                }
            } else {
                if (arrivalPin) {
                    map.removeLayer(arrivalPin);
                }
                arrivalPin = L.marker([lat, lon]).addTo(map);
                arrivalPin.bindPopup("Arrival: " + query).openPopup();

                // Update route points - arrival should be the last
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
            }

            // Use the new zoom pattern
            zoomInThenOut(lat, lon);

            // Draw the route if we have both departure and arrival points
            if (departurePin && arrivalPin) {
                // Clean up the route points to ensure departure is first and arrival is last
                reorganizeRoutePoints();
                drawSeaRoute();
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
        //setWaypointSelection(false);
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

            // Draw updated route
            if (departurePin && arrivalPin) {
                drawSeaRoute();
            }
        } else {
            alert("Port not found!");
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
}

// Function to ensure route points are properly organized
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
}

// Draw a sea route considering all the ports
function drawSeaRoute() {
    // Clear previous route
    routeLayer.clearLayers();

    if (routePoints.length < 2) {
        return; // Need at least departure and arrival to draw a route
    }

    // Collect all lat/lng coordinates in order
    const coordinates = routePoints.map(point => point.latLng);

    // Create a sea route through all points
    const seaRoute = createSeaRoute(coordinates);

    // Draw the route line
    L.polyline(seaRoute, {
        color: '#0066ff',
        weight: 3,
        opacity: 0.8,
        smoothFactor: 1,
        dashArray: null
    }).addTo(routeLayer);

    // Add distance markers between segments
    addDistanceMarkers(seaRoute);
    
    // Calculate bounds with right padding to account for 40% overlay
    const routeBounds = L.latLngBounds(seaRoute);

    // Calculate pixel padding - approximately 40% of map width to the left
    const paddingLeft = window.innerWidth * 0.45; // Adjust for overlay width

    // Fly to the bounds of the route with appropriate padding
    map.flyToBounds(routeBounds, {
        paddingTopLeft: [paddingLeft, 20],
        paddingBottomRight: [20, 20],
        duration: 1.5
    });
}

// Create a sea route that follows the ocean between ports
function createSeaRoute(coordinates) {
    const route = [];
    
    // For each segment between two points
    for (let i = 0; i < coordinates.length - 1; i++) {
        const start = coordinates[i];
        const end = coordinates[i + 1];
        
        // Start point
        route.push([start[0], start[1]]);
        
        // Add intermediate points to create a curved sea route
        // This is a simplified approach - in a real-world scenario,
        // you might use actual maritime route data or algorithms
        
        // Create a slight curve for the sea route
        const midLat = (start[0] + end[0]) / 2;
        const midLng = (start[1] + end[1]) / 2;
        
        // Add curvature depending on relation between points
        const latDiff = end[0] - start[0];
        const lngDiff = end[1] - start[1];
        
        // Determine if we need a northern or southern route
        const isMostlyEastWest = Math.abs(lngDiff) > Math.abs(latDiff);
        
        if (isMostlyEastWest) {
            // For east-west routes, add a slight deviation north or south
            // This simulates ships following shipping lanes
            const curveAmount = latDiff / 10;
            route.push([midLat + curveAmount, midLng]);
        } else {
            // For north-south routes, add a slight deviation east or west
            const curveAmount = lngDiff / 10;
            route.push([midLat, midLng + curveAmount]);
        }
    }
    
    // End point
    route.push([coordinates[coordinates.length - 1][0], coordinates[coordinates.length - 1][1]]);
    
    return route;
}
// New function to draw sea route from API coordinates
//function drawSeaRouteFromAPI(coordinates) {
//    // Clear previous route
//    routeLayer.clearLayers();

//    if (!coordinates || coordinates.length < 2) {
//        console.error("Not enough coordinates to draw a route");
//        return;
//    }

//    // Create a polyline from the coordinates
//    L.polyline(coordinates, {
//        color: '#0066ff',
//        weight: 3,
//        opacity: 0.8,
//        smoothFactor: 1,
//        dashArray: null
//    }).addTo(routeLayer);

//    // Add distance markers between segments
//    addDistanceMarkers(coordinates);

//    // Calculate bounds with right padding to account for 40% overlay
//    const routeBounds = L.latLngBounds(coordinates);

//    // Calculate pixel padding - approximately 40% of map width to the left
//    const paddingLeft = window.innerWidth * 0.45; // Adjust for overlay width

//    // Fly to the bounds of the route with appropriate padding
//    map.flyToBounds(routeBounds, {
//        paddingTopLeft: [paddingLeft, 20],
//        paddingBottomRight: [20, 20],
//        duration: 1.5
//    });
//}

// Function to draw sea route from API coordinates while preserving intermediate points
function drawSeaRouteFromAPI(apiCoordinates) {
    // Clear previous route from the route layer
    routeLayer.clearLayers();

    if (!apiCoordinates || apiCoordinates.length < 2) {
        console.error("Not enough coordinates to draw a route");
        return;
    }

    // Get current departure and arrival coordinates from existing pins
    const departureCoord = departurePin ? [departurePin.getLatLng().lat, departurePin.getLatLng().lng] : null;
    const arrivalCoord = arrivalPin ? [arrivalPin.getLatLng().lat, arrivalPin.getLatLng().lng] : null;

    // Get all intermediate waypoints and port coordinates
    const intermediatePoints = [];

    // Add port pins (in order they were added)
    portPins.forEach(pin => {
        intermediatePoints.push([pin.getLatLng().lat, pin.getLatLng().lng]);
    });

    // Add waypoint pins (in order they were added)
    waypointPins.forEach(pin => {
        intermediatePoints.push([pin.getLatLng().lat, pin.getLatLng().lng]);
    });

    // Create a complete route with all points
    let completeRoute = [];

    // Start with departure (either from pin or API)
    if (departureCoord) {
        completeRoute.push(departureCoord);
    } else if (apiCoordinates.length > 0) {
        completeRoute.push(apiCoordinates[0]);
    }

    // Add all intermediate points
    completeRoute = completeRoute.concat(intermediatePoints);

    // End with arrival (either from pin or API)
    if (arrivalCoord) {
        completeRoute.push(arrivalCoord);
    } else if (apiCoordinates.length > 1) {
        completeRoute.push(apiCoordinates[apiCoordinates.length - 1]);
    }

    // If we have API data but no user points, use the API route directly
    if (completeRoute.length <= 2 && apiCoordinates.length > 2) {
        completeRoute = apiCoordinates;
    }

    // Create a sea route through all points
    const seaRoute = createSeaRoute(completeRoute);

    // Draw the route line
    L.polyline(seaRoute, {
        color: '#0066ff',
        weight: 3,
        opacity: 0.8,
        smoothFactor: 1,
        dashArray: null
    }).addTo(routeLayer);

    // Add distance markers between segments
    addDistanceMarkers(seaRoute);

    // Calculate bounds with right padding to account for 40% overlay
    const routeBounds = L.latLngBounds(seaRoute);

    // Calculate pixel padding - approximately 40% of map width to the left
    const paddingLeft = window.innerWidth * 0.45; // Adjust for overlay width

    // Fly to the bounds of the route with appropriate padding
    map.flyToBounds(routeBounds, {
        paddingTopLeft: [paddingLeft, 20],
        paddingBottomRight: [20, 20],
        duration: 1.5
    });
}
// Add distance markers to the route
function addDistanceMarkers(routeCoordinates) {
    let totalDistance = 0;
    
    // Add markers between each segment
    for (let i = 0; i < routeCoordinates.length - 1; i++) {
        const start = L.latLng(routeCoordinates[i][0], routeCoordinates[i][1]);
        const end = L.latLng(routeCoordinates[i + 1][0], routeCoordinates[i + 1][1]);
        
        // Calculate distance for this segment
        const segmentDistance = calculateDistance(start, end);
        totalDistance += segmentDistance;
        
        // Add marker at the midpoint of this segment
        if (i > 0 && i < routeCoordinates.length - 2) {
            const midpoint = L.latLng(
                (start.lat + end.lat) / 2,
                (start.lng + end.lng) / 2
            );
            
            L.marker(midpoint, {
                icon: L.divIcon({
                    className: 'distance-marker',
                    html: `<div style="background-color: rgba(255, 255, 255, 0.8); padding: 2px 5px; border-radius: 3px; border: 1px solid #0066ff; font-size: 10px;">${segmentDistance} km</div>`,
                    iconSize: null
                })
            }).addTo(routeLayer);
        }
    }
    
    // Add total distance marker at the middle of the route
    const midIndex = Math.floor(routeCoordinates.length / 2);
    const midPoint = L.latLng(
        routeCoordinates[midIndex][0],
        routeCoordinates[midIndex][1]
    );
    
    L.marker(midPoint, {
        icon: L.divIcon({
            className: 'distance-marker',
            html: `<div style="background-color: white; padding: 3px 8px; border-radius: 4px; border: 1px solid #0066ff; font-weight: bold;">Total: ${Math.round(totalDistance)} km</div>`,
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

    // Reset waypoint selection
    isWaypointSelectionActive = false;
    map.getContainer().style.cursor = '';

    // Reset the map view to the initial state
    map.setView([20, 60], 3);
}

// Function to update the map with port data from API
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
        if (routePoints.length === 0) {
            routePoints.push({
                type: 'departure',
                latLng: [lat, lon],
                name: name
            });
        } else {
            routePoints[0] = {
                type: 'departure',
                latLng: [lat, lon],
                name: name
            };
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

    // Reorganize and draw route if we have departure and arrival
    if (departurePin && arrivalPin) {
        reorganizeRoutePoints();
        drawSeaRoute();
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

    // Reorganize and draw route if we have departure point
    if (departurePin) {
        reorganizeRoutePoints();
        drawSeaRoute();
    }
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