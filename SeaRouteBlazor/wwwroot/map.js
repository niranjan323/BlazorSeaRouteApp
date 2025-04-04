var map;
var departurePin;
var arrivalPin;
var routeLayer;
var isWaypointSelectionActive = false;
var coordTooltip;
var currentDotNetHelper;
let clickedPin = null;
let newportPin = null;

function initializeMap(dotNetHelper) {
    currentDotNetHelper = dotNetHelper;
    map = L.map('map').setView([20, 60], 3);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors', crossOrigin: 'anonymous' 
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

            // Remove existing pin if any
            if (clickedPin) {
                map.removeLayer(clickedPin);
            }

            // Add new pin at the clicked location
            clickedPin = L.marker([latitude, longitude]).addTo(map);
            clickedPin.bindPopup(`Waypoint: ${latitude.toFixed(5)}, ${longitude.toFixed(5)}`).openPopup();

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

async function zoomAndPinLocation(query, isDeparture) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();

        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);


            setTimeout(() => {
                // Add pin
                if (newportPin) {
                    map.removeLayer(newportPin);
                }
                newportPin = L.marker([lat, lon]).addTo(map);
                newportPin.bindPopup(`${isDeparture ? "new port" : "Arrival"}: ${query}`).openPopup();
            }, 2000);
            setTimeout(() => {

                map.flyTo([lat, lon], 3, { duration: 1.5 });

            }, 2000);

            map.flyTo([lat, lon], 8, { duration: 1.5 });
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
    if(clickedPin) {
        map.removeLayer(clickedPin);
        clickedPin = null;
    }
    if(newportPin) {
        map.removeLayer(newportPin);
        clickedPin = null;
    }
    // Reset waypoint selection
    isWaypointSelectionActive = false;
    map.getContainer().style.cursor = '';

    // Reset the map view to the initial state
    map.setView([20, 60], 3);
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

function updateMap(name, latitude, longitude) {
    if (!latitude || !longitude) return;

    let lat = parseFloat(latitude);
    let lon = parseFloat(longitude);

    if (isNaN(lat) || isNaN(lon)) return;

    let marker = L.marker([lat, lon]).addTo(map);
    marker.bindPopup(`${name}: ${lat.toFixed(5)}, ${lon.toFixed(5)}`).openPopup();

    // Adjust map view
    map.flyTo([lat, lon], 8, { duration: 1.5 });
}
