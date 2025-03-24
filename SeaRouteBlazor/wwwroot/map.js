var map;

function initializeMap() {
    map = L.map('map').setView([20, 60], 3);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);
}

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


