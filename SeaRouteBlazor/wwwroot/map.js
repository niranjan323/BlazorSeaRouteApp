var map;

function initializeMap(dotNetHelper) {
    map = L.map('map').setView([20, 60], 3);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    map.on('click', function (e) {
        var latitude = e.latlng.lat;
        var longitude = e.latlng.lng;

        console.log("Captured Coordinates:", latitude, longitude);

        // Call Blazor method
        dotNetHelper.invokeMethodAsync('CaptureCoordinates', latitude, longitude);
    });
}

async function searchLocation(query) {
    try {
        let response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${query}`);
        let data = await response.json();

        if (data.length > 0) {
            let lat = parseFloat(data[0].lat);
            let lon = parseFloat(data[0].lon);
            map.flyTo([lat + 5, lon + 5], 3, { duration: 1.5 });


            setTimeout(() => {
                
                map.flyTo([lat, lon], 1, { duration: 1.5 });
            }, 2000);
            map.flyTo([lat, lon], 8, { duration: 1.5 });
        } else {
            alert("Location not found!");
        }
    } catch (error) {
        console.error("Error fetching location:", error);
    }
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


function addMarker(lat, lon, label) {
    L.marker([lat, lon]).addTo(map).bindPopup(label).openPopup();
}

function drawRoute(coordinates) {
    L.polyline(coordinates, { color: 'blue' }).addTo(map);
}

function initializeGraph() {
    var canvas = document.getElementById('voyageGraph');
    if (!canvas) {
        console.error("voyageGraph element not found.");
        return;
    }

    var ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ["Day 1", "Day 2", "Day 3", "Day 4"],
            datasets: [{
                label: "Wave Height",
                data: [2, 3, 2.5, 3.5],
                borderColor: "blue",
                fill: false
            }]
        }
    });
}


