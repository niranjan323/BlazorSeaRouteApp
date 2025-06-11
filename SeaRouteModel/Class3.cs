
### Endpoint: POST /calculations/reduction-factors/raw

### Test 1: Annual ABS calculation
```json
{
  "dataSource": "ABS",
  "seasonType": "annual",
  "exceedanceProbability": 0.01,
  "segmentCoordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    },
    {
    "latitude": 51.5074,
      "longitude": -0.1278
    },
    {
    "latitude": 48.8566,
      "longitude": 2.3522
    }
  ]
}
```

### Test 2: Summer BMT calculation
```json
{
  "dataSource": "BMT",
  "seasonType": "summer",
  "exceedanceProbability": 0.05,
  "segmentCoordinates": [
    {
      "latitude": 35.6762,
      "longitude": 139.6503
    },
    {
    "latitude": 37.7749,
      "longitude": -122.4194
    }
  ]
}
```

### Test 3: Winter BMT calculation
```json
{
  "dataSource": "BMT",
  "seasonType": "winter",
  "exceedanceProbability": 0.02,
  "segmentCoordinates": [
    {
      "latitude": 55.7558,
      "longitude": 37.6173
    },
    {
    "latitude": 59.9311,
      "longitude": 30.3609
    },
    {
    "latitude": 60.1699,
      "longitude": 24.9384
    }
  ]
}
```

## Task 2: Segment Reduction Factors (All Seasons)
### Endpoint: POST /calculations/reduction-factors/segment

### Test 1: Without correction
```json
{
  "segmentCoordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    },
    {
    "latitude": 51.5074,
      "longitude": -0.1278
    }
  ],
  "exceedanceProbability": 0.01
}
```

### Test 2: With correction enabled
```json
{
  "segmentCoordinates": [
    {
      "latitude": 35.6762,
      "longitude": 139.6503
    },
    {
    "latitude": 37.7749,
      "longitude": -122.4194
    },
    {
    "latitude": 34.0522,
      "longitude": -118.2437
    }
  ],
  "exceedanceProbability": 0.05,
  "correction": true
}
```

### Test 3: High exceedance probability
```json
{
  "segmentCoordinates": [
    {
      "latitude": 25.7617,
      "longitude": -80.1918
    },
    {
    "latitude": 32.7767,
      "longitude": -96.7970
    }
  ],
  "exceedanceProbability": 0.1,
  "correction": false
}
```

## Task 4: Seasonal Factor Calculation
### Endpoint: POST /calculations/reduction-factors/seasonal-factor

### Test 1: Spring seasonal factor
```json
{
  "annualSignificantWaveHeight": 5.2,
  "rawReductionFactors": {
    "dataSource": "BMT",
    "seasonType": "spring",
    "exceedanceProbability": 0.01,
    "segmentCoordinates": [
      {
        "latitude": 40.7128,
        "longitude": -74.0060
      },
      {
        "latitude": 51.5074,
        "longitude": -0.1278
      }
    ]
  }
}
```

### Test 2: Fall seasonal factor
```json
{
  "annualSignificantWaveHeight": 4.8,
  "rawReductionFactors": {
    "dataSource": "BMT",
    "seasonType": "fall",
    "exceedanceProbability": 0.02,
    "segmentCoordinates": [
      {
        "latitude": 35.6762,
        "longitude": 139.6503
      },
      {
        "latitude": 37.7749,
        "longitude": -122.4194
      },
      {
        "latitude": 34.0522,
        "longitude": -118.2437
      }
    ]
  }
}
```

### Test 3: Winter seasonal factor (high wave height)
```json
{
  "annualSignificantWaveHeight": 6.5,
  "rawReductionFactors": {
    "dataSource": "BMT",
    "seasonType": "winter",
    "exceedanceProbability": 0.05,
    "segmentCoordinates": [
      {
        "latitude": 55.7558,
        "longitude": 37.6173
      },
      {
        "latitude": 59.9311,
        "longitude": 30.3609
      }
    ]
  }
}
```

## Error Test Cases

### Invalid DataSource
```json
{
  "dataSource": "INVALID",
  "seasonType": "annual",
  "exceedanceProbability": 0.01,
  "segmentCoordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    }
  ]
}
```

### Invalid Exceedance Probability (too high)
```json
{
  "dataSource": "ABS",
  "seasonType": "annual",
  "exceedanceProbability": 1.5,
  "segmentCoordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    }
  ]
}
```

### Invalid Exceedance Probability (too low)
```json
{
  "dataSource": "ABS",
  "seasonType": "annual",
  "exceedanceProbability": 0.0,
  "segmentCoordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    }
  ]
}
```

### Seasonal Factor with Annual Season (should fail)
```json
{
  "annualSignificantWaveHeight": 5.0,
  "rawReductionFactors": {
    "dataSource": "BMT",
    "seasonType": "annual",
    "exceedanceProbability": 0.01,
    "segmentCoordinates": [
      {
        "latitude": 40.7128,
        "longitude": -74.0060
      }
    ]
  }
}
```

## Legacy Endpoint Test (Backward Compatibility)
### Endpoint: POST /calculations/reduction-factors/reduction-factor-calculation/

### Test with old BkWxRouteRequest format
```json
{
  "dataSource": "ABS",
  "exceedanceProbability": 0.01,
  "coordinates": [
    {
      "latitude": 40.7128,
      "longitude": -74.0060
    },
    {
    "latitude": 51.5074,
      "longitude": -0.1278
    }
  ],
  "seasonType": "annual"
}
```

## Expected Response Examples

### Raw Reduction Factor Response
```json
{
  "significantWaveHeight": 4.5,
  "rawReductionFactor": 0.85
}
```

### Segment Reduction Factor Response
```json
{
  "annualRF": 0.85,
  "springRF": 0.82,
  "summerRF": 0.88,
  "fallRF": 0.83,
  "winterRF": 0.80
}
```

### Seasonal Factor Response
```json
{
  "seasonalFactor": 0.92,
  "rawReductionFactors": {
    "significantWaveHeight": 4.8,
    "rawReductionFactor": 0.87
  }
}
```