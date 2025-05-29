// Chart drawing utilities with proper Chart.js integration

// Main chart drawing function (consolidated from your two functions)
window.drawChart = (canvasId, reductionFactorData, chartIndex = 1) => {
    console.log(`Draw chart called for canvas: ${canvasId}, chart index: ${chartIndex}`);
    console.log("Received data type:", typeof reductionFactorData);
    console.log("Received data:", reductionFactorData);

    // Make sure Chart is defined
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded!");
        return false;
    }

    // Get canvas element
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error(`Canvas element with id '${canvasId}' not found!`);
        return false;
    }

    console.log("Canvas found:", canvas);

    // Get the 2D context
    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("Could not get 2D context from canvas!");
        return false;
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
        return false;
    }

    // Extract values (default values for Short Voyage Reduction Factor)
    const xValues = [0.00, 2.82, 8.48, 12.00];
    const yValues = [0.6, 0.6, 1.0, 1.0];

    // Extract common points
    let commonX, commonY;
    try {
        commonX = parseFloat(reductionFactor.commonX || reductionFactor.x || 3.2);
        commonY = parseFloat(reductionFactor.commonY || reductionFactor.y || 0.85);
        console.log("Common point:", commonX, commonY);
    } catch (e) {
        console.error("Error extracting common point:", e);
        commonX = 3.2;  // Default values
        commonY = 0.85;
    }

    // Create data points
    const lineData = xValues.map((x, i) => {
        return { x: parseFloat(x), y: parseFloat(yValues[i]) };
    });

    // Clean up any existing chart (use different variable names for multiple charts)
    const chartVariableName = `myChart${chartIndex}`;
    if (window[chartVariableName]) {
        window[chartVariableName].destroy();
    }

    // Create new chart
    window[chartVariableName] = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: [
                {
                    label: 'Short Voyage Reduction Factor',
                    data: lineData,
                    borderColor: 'blue',
                    borderWidth: 2,
                    fill: false,
                    tension: 0,
                    pointRadius: 4,
                    pointHoverRadius: 6
                },
                {
                    label: 'Hs,max (m)',
                    data: [{ x: commonX, y: commonY }],
                    backgroundColor: 'red',
                    borderColor: 'red',
                    type: 'scatter',
                    pointRadius: 8,
                    pointHoverRadius: 10
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                }
            },
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    title: {
                        display: true,
                        text: 'Forecast maximum significant wave height Hs,max(m)',
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: '#e0e0e0'
                    },
                    min: 0,
                    max: 14
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Short Voyage Reduction Factor',
                        font: {
                            size: 12
                        }
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

    console.log(`Chart ${chartIndex} created successfully!`);
    return true;
};

// Specific function for chart 1 (backwards compatibility)
window.drawChart1 = (canvasId, reductionFactorData) => {
    return window.drawChart(canvasId, reductionFactorData, 1);
};

// Specific function for chart 2 (backwards compatibility)
window.drawChart2 = (canvasId, reductionFactorData) => {
    return window.drawChart(canvasId, reductionFactorData, 2);
};

// Get chart as base64 image
window.getChartAsBase64 = (canvasId) => {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found!`);
            return null;
        }

        return canvas.toDataURL('image/png');
    } catch (error) {
        console.error('Error getting chart as base64:', error);
        return null;
    }
};

// Destroy chart by canvas ID
window.destroyChart = (canvasId, chartIndex = 1) => {
    try {
        const chartVariableName = `myChart${chartIndex}`;
        if (window[chartVariableName]) {
            window[chartVariableName].destroy();
            window[chartVariableName] = null;
            console.log(`Chart ${chartIndex} destroyed`);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error destroying chart:', error);
        return false;
    }
};

// Resize chart
window.resizeChart = (canvasId, chartIndex = 1) => {
    try {
        const chartVariableName = `myChart${chartIndex}`;
        if (window[chartVariableName]) {
            window[chartVariableName].resize();
            console.log(`Chart ${chartIndex} resized`);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error resizing chart:', error);
        return false;
    }
};