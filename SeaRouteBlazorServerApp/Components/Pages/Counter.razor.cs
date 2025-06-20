// Modified by Niranjan - Added new classes and methods for route splitting and longitude normalization

// New classes for route splitting functionality
using SeaRouteModel.Models;
using System.Text.Json;
using static NextGenEngApps.DigitalRules.CRoute.Components.Pages.ReductionFactorCalculation;

public class RoutePoint
{
    public int RoutePointOrder { get; set; }
    public string RoutePointType { get; set; } // "port" or "waypoint"
    public string GeoPointId { get; set; }
    public double SegDistance { get; set; }
    public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
}

public class VoyageLeg
{
    public string StartPortId { get; set; }
    public string EndPortId { get; set; }
    public string StartPortName { get; set; }
    public string EndPortName { get; set; }
    public double TotalDistance { get; set; }
    public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
}

// Modified by Niranjan - Task 2: Longitude normalization method
/// <summary>
/// Converts longitude values into the range of [-180, 180)
/// </summary>
/// <param name="coordinates">List of coordinates to normalize</param>
/// <returns>New list with normalized longitude values</returns>
private List<Coordinate> NormalizeLongitudeValues(List<Coordinate> coordinates)
{
    if (coordinates == null || coordinates.Count == 0)
        return coordinates;

    List<Coordinate> normalizedCoordinates = new List<Coordinate>();

    foreach (var coord in coordinates)
    {
        double normalizedLng = NormalizeLongitude(coord.Longitude);

        normalizedCoordinates.Add(new Coordinate
        {
            Longitude = normalizedLng,
            Latitude = coord.Latitude
        });
    }

    return normalizedCoordinates;
}

// Modified by Niranjan - Task 2: Core longitude normalization algorithm
/// <summary>
/// Normalizes a single longitude value to the range [-180, 180)
/// </summary>
/// <param name="longitude">The longitude value to normalize</param>
/// <returns>Normalized longitude value</returns>
private double NormalizeLongitude(double longitude)
{
    const double T = 360.0; // Period
    const double t0 = -180.0; // Starting bound

    // Calculate k using the formula: k = Math.Floor((alpha - t0) / T)
    int k = (int)Math.Floor((longitude - t0) / T);

    // Calculate normalized longitude: alpha_0 = alpha - k * T
    double normalizedLongitude = longitude - k * T;

    return normalizedLongitude;
}

// Modified by Niranjan - Task 1: Main route splitting method
/// <summary>
/// Splits the route into voyage legs based on port locations
/// </summary>
/// <param name="routePoints">List of route points with their coordinates</param>
/// <returns>List of voyage legs</returns>
private List<VoyageLeg> SplitRouteIntoVoyageLegs(List<RoutePoint> routePoints)
{
    if (routePoints == null || routePoints.Count == 0)
        return new List<VoyageLeg>();

    List<VoyageLeg> voyageLegs = new List<VoyageLeg>();
    VoyageLeg currentLeg = null;

    // Sort route points by order to ensure correct sequence
    var sortedRoutePoints = routePoints.OrderBy(rp => rp.RoutePointOrder).ToList();

    for (int i = 0; i < sortedRoutePoints.Count; i++)
    {
        var currentPoint = sortedRoutePoints[i];

        if (currentPoint.RoutePointType.ToLower() == "port")
        {
            // Create a new voyage leg
            currentLeg = new VoyageLeg();

            // Set start port information
            if (i == 0) // First point
            {
                currentLeg.StartPortId = currentPoint.GeoPointId;
                currentLeg.StartPortName = GetPortNameById(currentPoint.GeoPointId);
            }
            else
            {
                // Find the previous port to set as start
                var previousPort = FindPreviousPort(sortedRoutePoints, i);
                if (previousPort != null)
                {
                    currentLeg.StartPortId = previousPort.GeoPointId;
                    currentLeg.StartPortName = GetPortNameById(previousPort.GeoPointId);
                }
            }

            // Set end port information
            currentLeg.EndPortId = currentPoint.GeoPointId;
            currentLeg.EndPortName = GetPortNameById(currentPoint.GeoPointId);

            // Initialize with current point's segment data
            currentLeg.TotalDistance = currentPoint.SegDistance;
            currentLeg.Coordinates = new List<Coordinate>(currentPoint.Coordinates);

            voyageLegs.Add(currentLeg);
        }
        else if (currentPoint.RoutePointType.ToLower() == "waypoint" && currentLeg != null)
        {
            // Add waypoint data to the current voyage leg
            currentLeg.TotalDistance += currentPoint.SegDistance;
            currentLeg.Coordinates.AddRange(currentPoint.Coordinates);
        }

        // Stop if this is the last route point
        if (i == sortedRoutePoints.Count - 1)
            break;
    }

    // Modified by Niranjan - Process voyage legs for longitude normalization and duplicate removal
    ProcessVoyageLegsCoordinates(voyageLegs);

    return voyageLegs;
}

// Modified by Niranjan - Helper method to find previous port
private RoutePoint FindPreviousPort(List<RoutePoint> routePoints, int currentIndex)
{
    for (int i = currentIndex - 1; i >= 0; i--)
    {
        if (routePoints[i].RoutePointType.ToLower() == "port")
        {
            return routePoints[i];
        }
    }
    return null;
}

// Modified by Niranjan - Process voyage legs coordinates
/// <summary>
/// Processes voyage leg coordinates by normalizing longitude and removing duplicates
/// </summary>
/// <param name="voyageLegs">List of voyage legs to process</param>
private void ProcessVoyageLegsCoordinates(List<VoyageLeg> voyageLegs)
{
    foreach (var leg in voyageLegs)
    {
        if (leg.Coordinates != null && leg.Coordinates.Count > 0)
        {
            // Step 1: Normalize longitude values
            leg.Coordinates = NormalizeLongitudeValues(leg.Coordinates);

            // Step 2: Remove duplicate coordinates
            leg.Coordinates = RemoveDuplicateCoordinates(leg.Coordinates);
        }
    }
}

// Modified by Niranjan - Convert route model to route points
/// <summary>
/// Converts the current route model to a list of route points
/// </summary>
/// <returns>List of route points with coordinates</returns>
private List<RoutePoint> ConvertRouteModelToRoutePoints()
{
    List<RoutePoint> routePoints = new List<RoutePoint>();
    int order = 1;

    // Add main departure port
    if (routeModel.MainDeparturePortSelection?.Port != null)
    {
        var routePoint = new RoutePoint
        {
            RoutePointOrder = order++,
            RoutePointType = "port",
            GeoPointId = routeModel.MainDeparturePortSelection.Port.Port_Id,
            SegDistance = GetDistanceFromRouteSegment(routeModel.MainDeparturePortSelection.Port.Port_Id),
            Coordinates = GetCoordinatesForPoint(routeModel.MainDeparturePortSelection.Port.Port_Id)
        };
        routePoints.Add(routePoint);
    }

    // Add departure items (ports and waypoints)
    if (routeModel.DepartureItems != null)
    {
        foreach (var item in routeModel.DepartureItems)
        {
            var routePoint = new RoutePoint
            {
                RoutePointOrder = order++,
                RoutePointType = item.ItemType == "P" ? "port" : "waypoint",
                GeoPointId = item.ItemType == "P" ? item.Port.Port.Port_Id : item.Waypoint.PointId,
                SegDistance = item.ItemType == "P" ?
                    GetDistanceFromRouteSegment(item.Port.Port.Port_Id) :
                    GetDistanceFromRouteSegment(item.Waypoint.PointId),
                Coordinates = item.ItemType == "P" ?
                    GetCoordinatesForPoint(item.Port.Port.Port_Id) :
                    GetCoordinatesForPoint(item.Waypoint.PointId)
            };
            routePoints.Add(routePoint);
        }
    }

    // Add arrival items (ports and waypoints)
    if (routeModel.ArrivalItems != null)
    {
        foreach (var item in routeModel.ArrivalItems)
        {
            var routePoint = new RoutePoint
            {
                RoutePointOrder = order++,
                RoutePointType = item.ItemType == "P" ? "port" : "waypoint",
                GeoPointId = item.ItemType == "P" ? item.Port.Port.Port_Id : item.Waypoint.PointId,
                SegDistance = item.ItemType == "P" ?
                    GetDistanceFromRouteSegment(item.Port.Port.Port_Id) :
                    GetDistanceFromRouteSegment(item.Waypoint.PointId),
                Coordinates = item.ItemType == "P" ?
                    GetCoordinatesForPoint(item.Port.Port.Port_Id) :
                    GetCoordinatesForPoint(item.Waypoint.PointId)
            };
            routePoints.Add(routePoint);
        }
    }

    // Add main arrival port
    if (routeModel.MainArrivalPortSelection?.Port != null)
    {
        var routePoint = new RoutePoint
        {
            RoutePointOrder = order++,
            RoutePointType = "port",
            GeoPointId = routeModel.MainArrivalPortSelection.Port.Port_Id,
            SegDistance = 0, // Last point has no distance to next
            Coordinates = new List<Coordinate>() // Last point has no segment coordinates
        };
        routePoints.Add(routePoint);
    }

    return routePoints;
}

// Modified by Niranjan - Helper method to get coordinates for a point
/// <summary>
/// Gets coordinates for a specific point from route segments
/// </summary>
/// <param name="pointId">The point ID to get coordinates for</param>
/// <returns>List of coordinates for the point</returns>
private List<Coordinate> GetCoordinatesForPoint(string pointId)
{
    if (routeModel.RouteSegments == null)
        return new List<Coordinate>();

    var segment = routeModel.RouteSegments.FirstOrDefault(s => s.StartPointId == pointId);
    if (segment != null)
    {
        // Return coordinates from the segment starting at this point
        // For now, returning start and end coordinates
        // In a real implementation, you would have the full path coordinates
        return new List<Coordinate>
        {
            new Coordinate
            {
                Latitude = segment.StartCoordinates[1],
                Longitude = segment.StartCoordinates[0]
            },
            new Coordinate
            {
                Latitude = segment.EndCoordinates[1],
                Longitude = segment.EndCoordinates[0]
            }
        };
    }

    return new List<Coordinate>();
}

// Modified by Niranjan - Helper method to get port name by ID
/// <summary>
/// Gets port name by port ID
/// </summary>
/// <param name="portId">The port ID</param>
/// <returns>Port name</returns>
private string GetPortNameById(string portId)
{
    // Check main departure port
    if (routeModel.MainDeparturePortSelection?.Port?.Port_Id == portId)
        return routeModel.MainDeparturePortSelection.Port.Name;

    // Check main arrival port
    if (routeModel.MainArrivalPortSelection?.Port?.Port_Id == portId)
        return routeModel.MainArrivalPortSelection.Port.Name;

    // Check departure items
    if (routeModel.DepartureItems != null)
    {
        var departurePort = routeModel.DepartureItems
            .Where(item => item.ItemType == "P" && item.Port?.Port?.Port_Id == portId)
            .FirstOrDefault();
        if (departurePort != null)
            return departurePort.Port.Port.Name;
    }

    // Check arrival items
    if (routeModel.ArrivalItems != null)
    {
        var arrivalPort = routeModel.ArrivalItems
            .Where(item => item.ItemType == "P" && item.Port?.Port?.Port_Id == portId)
            .FirstOrDefault();
        if (arrivalPort != null)
            return arrivalPort.Port.Port.Name;
    }

    return $"Port {portId}"; // Default name if not found
}

// Modified by Niranjan - Updated ShowRouteItems method to use voyage legs
public async Task ShowRouteItemsWithVoyageLegs(List<Coordinate> coordinates = null)
{
    try
    {
        // Get overall reduction factor using provided coordinates from API if available
        if (coordinates != null && coordinates.Count > 0)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var rf = await GetReductionFactor(coordinates);
            stopwatch.Stop();

            var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;
            await JS.InvokeVoidAsync("console.log", $"Total execution time overall reduction factor: {totalTimeTaken} s");
            routeModel.ReductionFactor = rf;
        }

        bool isSimpleRoute = IsSimpleRouteWithoutIntermediatePoints();
        if (isSimpleRoute)
        {
            routeLegs.Clear();
            return;
        }

        // Modified by Niranjan - Use route splitting for voyage legs
        var routePoints = ConvertRouteModelToRoutePoints();
        var voyageLegs = SplitRouteIntoVoyageLegs(routePoints);

        // Clear existing route legs and populate with voyage legs
        routeLegs.Clear();

        foreach (var voyageLeg in voyageLegs)
        {
            // Calculate reduction factor for each voyage leg
            if (voyageLeg.Coordinates != null && voyageLeg.Coordinates.Count > 0)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                double reductionFactor = await GetReductionFactor(voyageLeg.Coordinates);
                stopwatch.Stop();

                var totalTimeTaken = stopwatch.Elapsed.TotalSeconds;
                await JS.InvokeVoidAsync("console.log", $"Total execution time voyage leg: {totalTimeTaken} s");

                var routeLeg = new RouteLegModel()
                {
                    DeparturePortId = voyageLeg.StartPortId,
                    ArrivalPortId = voyageLeg.EndPortId,
                    DeparturePort = voyageLeg.StartPortName,
                    ArrivalPort = voyageLeg.EndPortName,
                    Distance = voyageLeg.TotalDistance,
                    ReductionFactor = reductionFactor
                };

                routeLegs.Add(routeLeg);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in ShowRouteItemsWithVoyageLegs: {ex.Message}");
        throw;
    }
}

// Modified by Niranjan - Updated calculation method to use voyage legs
public async Task CalculateMultiSegmentRouteWithVoyageLegs()
{
    try
    {
        // Initialize route calculation on JS side but don't use its data
        await JS.InvokeVoidAsync("initializeRouteCalculation");

        // Get route points from JS for ordering/sequence purposes only
        var routePoints = await JS.InvokeAsync<List<RoutePointModel>>("getRouteData");
        if (routePoints == null || routePoints.Count < 2)
        {
            return; // Need at least 2 points for a route
        }

        // Create a mapping of all points for processing
        Dictionary<string, RoutePointRef> pointMapping = new Dictionary<string, RoutePointRef>();

        List<RouteSegmentInfo> routeSegments = new List<RouteSegmentInfo>();
        double totalDistance = 0;
        double totalDuration = 0;

        // Add departure port
        if (routeModel.MainDeparturePortSelection?.Port != null)
        {
            pointMapping["departure"] = new RoutePointRef
            {
                Type = "departure",
                Latitude = routeModel.MainDeparturePortSelection.Port.Latitude,
                Longitude = routeModel.MainDeparturePortSelection.Port.Longitude,
                Name = routeModel.MainDeparturePortSelection.Port.Name,
                PointId = routeModel.MainDeparturePortSelection.Port.Port_Id
            };
        }

        // Add arrival port
        if (routeModel.MainArrivalPortSelection?.Port != null)
        {
            pointMapping["arrival"] = new RoutePointRef
            {
                Type = "arrival",
                Latitude = routeModel.MainArrivalPortSelection.Port.Latitude,
                Longitude = routeModel.MainArrivalPortSelection.Port.Longitude,
                Name = routeModel.MainArrivalPortSelection.Port.Name,
                PointId = routeModel.MainArrivalPortSelection.Port.Port_Id
            };
        }

        // Add intermediate departure ports
        for (int i = 0; i < routeModel.DeparturePorts.Count; i++)
        {
            var port = routeModel.DeparturePorts[i];
            if (port.Port != null)
            {
                pointMapping[$"departurePort{i}"] = new RoutePointRef
                {
                    Type = "port",
                    Latitude = port.Port.Latitude,
                    Longitude = port.Port.Longitude,
                    Name = port.Port.Name,
                    PointId = port.Port.Port_Id
                };
            }
        }

        // Add intermediate arrival ports
        for (int i = 0; i < routeModel.ArrivalPorts.Count; i++)
        {
            var port = routeModel.ArrivalPorts[i];
            if (port.Port != null)
            {
                pointMapping[$"arrivalPort{i}"] = new RoutePointRef
                {
                    Type = "port",
                    Latitude = port.Port.Latitude,
                    Longitude = port.Port.Longitude,
                    Name = port.Port.Name,
                    PointId = port.Port.Port_Id
                };
            }
        }

        // Process waypoints from C# models
        // Add departure waypoints
        for (int i = 0; i < routeModel.DepartureWaypoints.Count; i++)
        {
            var waypoint = routeModel.DepartureWaypoints[i];
            if (double.TryParse(waypoint.Latitude, out double lat) &&
                double.TryParse(waypoint.Longitude, out double lng))
            {
                pointMapping[$"departureWaypoint{i}"] = new RoutePointRef
                {
                    Type = "waypoint",
                    Latitude = lat,
                    Longitude = lng,
                    Name = $"Waypoint {i + 1}",
                    PointId = waypoint.PointId
                };
            }
        }

        // Add arrival waypoints
        for (int i = 0; i < routeModel.ArrivalWaypoints.Count; i++)
        {
            var waypoint = routeModel.ArrivalWaypoints[i];
            if (double.TryParse(waypoint.Latitude, out double lat) &&
                double.TryParse(waypoint.Longitude, out double lng))
            {
                pointMapping[$"arrivalWaypoint{i}"] = new RoutePointRef
                {
                    Type = "waypoint",
                    Latitude = lat,
                    Longitude = lng,
                    Name = $"Waypoint {i + 1}",
                    PointId = waypoint.PointId
                };
            }
        }

        // Process each segment using route points order from JS but data from C#
        List<RoutePointRef> orderedPoints = new List<RoutePointRef>();

        // Use JS route points to determine order but get actual data from C# models
        foreach (var jsPoint in routePoints)
        {
            RoutePointRef point = null;

            // Try to match point from JS with our C# data
            if (jsPoint.Type == "departure" && pointMapping.ContainsKey("departure"))
            {
                point = pointMapping["departure"];
            }
            else if (jsPoint.Type == "arrival" && pointMapping.ContainsKey("arrival"))
            {
                point = pointMapping["arrival"];
            }
            else if (jsPoint.Type == "waypoint")
            {
                // Find closest waypoint in our mapping by comparing coordinates
                string closestKey = null;
                double minDistance = double.MaxValue;

                foreach (var kvp in pointMapping)
                {
                    if (kvp.Value.Type == "waypoint")
                    {
                        double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
                                      Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestKey = kvp.Key;
                        }
                    }
                }

                if (closestKey != null)
                {
                    point = pointMapping[closestKey];
                }
            }
            else if (jsPoint.Type == "port")
            {
                // Find closest port in our mapping
                string closestKey = null;
                double minDistance = double.MaxValue;

                foreach (var kvp in pointMapping)
                {
                    if (kvp.Value.Type == "port")
                    {
                        double dist = Math.Pow(kvp.Value.Latitude - jsPoint.LatLng[0], 2) +
                                      Math.Pow(kvp.Value.Longitude - jsPoint.LatLng[1], 2);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestKey = kvp.Key;
                        }
                    }
                }

                if (closestKey != null)
                {
                    point = pointMapping[closestKey];
                }
            }

            if (point != null)
            {
                orderedPoints.Add(point);
            }
        }

        bool hasDeparture = orderedPoints.Count > 0 &&
                 (orderedPoints[0].Type == "departure" || routePoints[0].Type == "departure");

        if (hasDeparture && orderedPoints.Count >= 2)
        {
            // Process each segment sequentially using our ordered C# data
            for (int i = 0; i < orderedPoints.Count - 1; i++)
            {
                var origin = orderedPoints[i];
                var destination = orderedPoints[i + 1];

                // Create route request for this segment
                var segmentRequest = new RouteRequest
                {
                    // Use coordinates from C# model
                    Origin = new double[] { origin.Longitude, origin.Latitude },
                    Destination = new double[] { destination.Longitude, destination.Latitude },
                    Restrictions = new string[] { "northwest" },
                    include_ports = false,
                    Units = "nm",
                    only_terminals = true
                };

                // Call API for this segment
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(Configuration["ApiUrl"]);
                var result = await httpClient.PostAsJsonAsync("api/v1/searoutes/calculate-route", segmentRequest);

                if (result.IsSuccessStatusCode)
                {
                    var jsonString = await result.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;
                    extractedCoordinates = new List<Coordinate>();

                    // Check if route object exists
                    if (root.TryGetProperty("route", out var routeElement) &&
                        routeElement.TryGetProperty("properties", out var propertiesElement))
                    {
                        // Extract segment distance and duration
                        double segmentDistance = 0;
                        double segmentDuration = 0;
                        string units = "km";

                        if (routeElement.TryGetProperty("geometry", out var geometryElement) &&
                            geometryElement.TryGetProperty("coordinates", out var coordinatesElement) &&
                            coordinatesElement.ValueKind == JsonValueKind.Array)
                        {
                            // Process each coordinate in the array
                            foreach (var coord in coordinatesElement.EnumerateArray())
                            {
                                if (coord.ValueKind == JsonValueKind.Array && coord.GetArrayLength() >= 2)
                                {
                                    // Note: GeoJSON format is [longitude, latitude]
                                    var longitude = coord[0].GetDouble();
                                    var latitude = coord[1].GetDouble();

                                    extractedCoordinates.Add(new Coordinate
                                    {
                                        Latitude = latitude,
                                        Longitude = longitude
                                    });
                                }
                            }
                        }

                        // Modified by Niranjan - Apply longitude normalization to extracted coordinates
                        extractedCoordinates = NormalizeLongitudeValues(extractedCoordinates);

                        if (propertiesElement.TryGetProperty("length", out var lengthElement))
                        {
                            segmentDistance = lengthElement.GetDouble();
                            totalDistance += segmentDistance;
                        }

                        if (propertiesElement.TryGetProperty("duration_hours", out var durationElement))
                        {
                            segmentDuration = durationElement.GetDouble();
                            totalDuration += segmentDuration;
                        }

                        if (propertiesElement.TryGetProperty("units", out var unitsElement))
                        {
                            units = unitsElement.GetString();
                        }

                        // Store segment information
                        var segmentInfo = new RouteSegmentInfo
                        {
                            SegmentIndex = i,
                            StartPointName = origin.Name,
                            EndPointName = destination.Name,
                            Distance = segmentDistance,
                            DurationHours = segmentDuration,
                            Units = units,
                            StartCoordinates = new double[] { origin.Longitude, origin.Latitude },
                            EndCoordinates = new double[] { destination.Longitude, destination.Latitude },
                            StartPointId = origin.PointId,
                            EndPointId = destination.PointId,
                        };

                        routeSegments.Add(segmentInfo);

                        // Get the raw JSON for the route
                        var routeJson = routeElement.GetRawText();

                        // Pass segment info to JavaScript along with route data
                        await JS.InvokeVoidAsync("processRouteSegment", routeJson, i, orderedPoints.Count - 1);
                    }
                }
                else
                {
                    Console.WriteLine($"Error calculating route segment {i}: {result.StatusCode}");
                }
            }
        }

        routeModel.RouteSegments = routeSegments;
        routeModel.TotalDistance = totalDistance;
        routeModel.TotalDurationHours = totalDuration;

        // Modified by Niranjan - After calculating segments, process voyage legs for reduction factor calculation
        await ShowRouteItemsWithVoyageLegs(extractedCoordinates);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error calculating multi-segment route: {ex.Message}");
    }
}

// Modified by Niranjan - Method to send voyage legs to RF API
/// <summary>
/// Sends voyage legs to the reduction factor calculation API
/// </summary>
/// <param name="voyageLegs">List of voyage legs to send</param>
/// <returns>Task representing the async operation</returns>
private async Task SendVoyageLegsToRFAPI(List<VoyageLeg> voyageLegs)
{
    try
    {
        foreach (var leg in voyageLegs)
        {
            if (leg.Coordinates != null && leg.Coordinates.Count > 0)
            {
                var request = new RFCalculationRequest()
                {
                    PointNumber = leg.Coordinates.Count,
                    Coordinates = leg.Coordinates,
                    ExceedanceProbability = routeModel.ExceedanceProbability ?? 0,
                    WaveType = "ABS"
                };

                // Send to RF API
                var response = await routeAPIService.GetReductionFactor(request);

                if (response != null)
                {
                    // Update the corresponding route leg with the reduction factor
                    var routeLeg = routeLegs.FirstOrDefault(rl =>
                        rl.DeparturePortId == leg.StartPortId &&
                        rl.ArrivalPortId == leg.EndPortId);

                    if (routeLeg != null)
                    {
                        routeLeg.ReductionFactor = response.ReductionFactor;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending voyage legs to RF API: {ex.Message}");
    }
}