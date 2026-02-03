using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;

private string GenerateHtmlFromDataCollector(ReportDataCollector dataCollector, bool isCLPVChecked = false)
{
    string html = "";
    html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{dataCollector.ReportTitle}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 20px;
            line-height: 1.6;
            color: #333;
            background-color: #f8f9fa;
        }}
        .report-container {{
            max-width: 1000px;
            margin: 0 auto;
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 2px solid #0066cc;
        }}
  .route-image-section h5 {{
            color: #333;
            font-size: 18px;
            margin-bottom: 15px;
            border-bottom: 1px solid #eee;
            padding-bottom: 5px;
  }}
  .route-image-section img {{
    display: inline-block;
    max-width: 100%;
    height: auto;
    border: 1px solid #ccc;
    border-radius: 4px;
  }}
        .report-section {{
            margin-bottom: 35px;
        }}
        .report-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            background-color: white;
        }}
        .report-table th, .report-table td {{
            border: 1px solid #ddd;
            padding: 12px 8px;
            text-align: left;
        }}
        .report-table th {{
            background-color: #f2f2f2;
            font-weight: bold;
            color: #333;
        }}
        .highlight {{
            color: #0066cc;
            font-weight: 500;
        }}
        .reduction-factor {{
            font-weight: bold;
            color: #0066cc;
        }}
        h4 {{
            color: #333;
            font-size: 24px;
            margin-bottom: 10px;
        }}
        h5 {{
            color: #333;
            font-size: 18px;
            margin-bottom: 15px;
            border-bottom: 1px solid #eee;
            padding-bottom: 5px;
        }}
        .notes-section ul {{
            padding-left: 20px;
        }}
        .notes-section li {{
            margin-bottom: 8px;
        }}
        .seasonal-table {{
            width: 100%;
            margin-top: 20px;
        }}
        .seasonal-table th {{
            background-color: #e3f2fd;
            color: #0066cc;
            text-align: center;
            font-size: 12px;
        }}
        .seasonal-header {{
            background-color: #e3f2fd !important;
            color: #0066cc !important;
            font-weight: bold;
        }}
        .route-segment-cell {{
            font-weight: 500;
        }}
        .attention-box {{
            background-color: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 4px;
            padding: 15px;
            margin-bottom: 20px;
        }}
        .download-timestamp {{
            color: #666;
            font-size: 14px;
        }}
        .route-analysis-section {{
            overflow-x: auto;
        }}
        .entire-route-row {{
            background-color: #f8f9fa;
            font-weight: bold;
        }}
        .route-splitting-row {{
            background-color: #ffffff;
        }}
        .season-months {{
            font-size: 10px;
            color: #999;
            font-style: italic;
        }}
        @media print {{
            body {{ background-color: white; }}
            .report-container {{ box-shadow: none; }}
        }}
    </style>
</head>
<body>
    <div class=""report-container"">

        <!-- Header -->
        <div class=""header"">
            <h4>{dataCollector.ReportTitle}</h4>
            <p class=""download-timestamp""><strong>Downloaded:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>";

    if (
        !string.IsNullOrEmpty(dataCollector.AttentionBlock?.DeparturePort) ||
        !string.IsNullOrEmpty(dataCollector.AttentionBlock?.ArrivalPort))
    {
        html += $@"
        <!-- Attention Section -->
        <div class=""report-section"">
            <div class=""attention-box"">";

        if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.DeparturePort) &&
            !string.IsNullOrEmpty(dataCollector.AttentionBlock?.ArrivalPort))
        {
            html += $@"
                <p>
                    {AttentionBlock.BuildAttentionBody(
                        dataCollector.AttentionBlock.DeparturePort,
                        dataCollector.AttentionBlock.ArrivalPort,
                        dataCollector.AttentionBlock.ReductionFactor.ToString("F2"))}
                </p>";
        }

        if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.ABSContact))
        {
            html += $@"<p>{dataCollector.AttentionBlock.ABSContact}</p>";
        }

        html += @"
            </div>
        </div>";
    }

    html += @"
        <!-- User Inputs Section -->
        <div class=""report-section"">
            <h5>User Inputs</h5>
            <table class=""report-table"">";

    if (!string.IsNullOrEmpty(dataCollector.ReportInfo?.ReportName))
    {
        html += $@"
                <tr>
                    <td><strong>Route Name:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo.ReportName}</td>
                    <td></td>
                </tr>";
    }

    if (dataCollector.ReportInfo?.ReportDate != default)
    {
        html += $@"
                <tr>
                    <td><strong>Report Date:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo.ReportDate:yyyy-MM-dd}</td>
                    <td></td>
                </tr>";
    }

    if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.VesselName))
    {
        html += $@"
                <tr>
                    <td><strong>Vessel Name:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.VesselName}</td>
                    <td></td>
                </tr>";
    }

    if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.IMONumber))
    {
        html += $@"
                <tr>
                    <td><strong>Vessel IMO:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.IMONumber}</td>
                    <td></td>
                </tr>";
    }

    html += $@"
                <tr>
                    <td><strong>Flag:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo?.Flag ?? ""}</td>
                    <td></td>
                </tr>";


    if (dataCollector.RouteInfo?.Ports != null && dataCollector.RouteInfo.Ports.Any())
    {
        for (int i = 0; i < dataCollector.RouteInfo.Ports.Count; i++)
        {
            var port = dataCollector.RouteInfo.Ports[i];
            string portType;

            if (i == 0)
                portType = "Port of Departure:";
            else if (i == dataCollector.RouteInfo.Ports.Count - 1)
                portType = "Port of Arrival:";
            else
                portType = $"Loading Port {i}:";

            html += $@"
                <tr>
                    <td><strong>{portType}</strong></td>
                    <td class=""highlight"">{port.Unlocode ?? "N/A"}</td>
                    <td>{port.Name ?? ""}</td>
                </tr>";
        }
    }

    html += @"
            </table>
        </div>";

    if (dataCollector.ReductionFactorResults != null && dataCollector.ReductionFactorResults.Any())
    {

        if (isCLPVChecked)
        {
            var completeroute = dataCollector.ReductionFactorResults.FirstOrDefault(r => r.VoyageLegOrder == 0);
            if (completeroute != null)
            {


                html += $@"
                     <!-- Output Section -->
                   <div class=""report-section route-analysis-section"">
                   <h5>Output</h5>
                     <p class=""clpv-reduction-factor"">
                     <strong>Reduction Factor: </strong>
                     <span class=""reduction-factor"">{Math.Round(completeroute.ReductionFactors.Annual, 2):F2}</span>
                     </p>";
            }
        }
        else
        {

            html += @"
        <!-- Output Section -->
        <div class=""report-section route-analysis-section"">
            <h5>Output</h5>

            <table class=""seasonal-table report-table"">
                <thead>
                    <tr>
                        <th rowspan=""2""></th>
                        <th colspan=""3""></th>
                        <th colspan=""4"" class=""seasonal-header"">Seasonal Reduction Factor</th>
                    </tr>
                    <tr>
                        <th class=""seasonal-header"">Routes</th>
                        <th class=""seasonal-header"">Distance</th>
                        <th class=""seasonal-header"">Annual Reduction Factor</th>
                        <th>Spring<br/><span class=""season-months"">(Mar-May)</span></th>
                        <th>Summer<br/><span class=""season-months"">(Jun-Aug)</span></th>
                        <th>Fall<br/><span class=""season-months"">(Sep-Nov)</span></th>
                        <th>Winter<br/><span class=""season-months"">(Dec-Feb)</span></th>
                    </tr>
                </thead>
                <tbody>";

            var entireRoute = dataCollector.ReductionFactorResults.FirstOrDefault(r => r.VoyageLegOrder == 0);
            if (entireRoute != null)
            {
                html += $@"
                    <tr class=""entire-route-row"">
                        <td><strong>Entire Route</strong></td>
                        <td class=""route-segment-cell"">{entireRoute.DeparturePort?.Name ?? ""} - {entireRoute.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(entireRoute.Distance)} nm</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Winter:F2}</td>
                    </tr>";
            }

            var routeLegs = dataCollector.ReductionFactorResults.Where(r => r.VoyageLegOrder > 0).OrderBy(r => r.VoyageLegOrder).ToList();
            if (routeLegs.Any())
            {
                var firstLeg = routeLegs.First();
                html += $@"
                    <tr class=""route-splitting-row"">
                        <td rowspan=""{routeLegs.Count}""><strong>Route Splitting</strong></td>
                        <td class=""route-segment-cell"">{firstLeg.DeparturePort?.Name ?? ""} - {firstLeg.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(firstLeg.Distance)} nm</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Winter:F2}</td>
                    </tr>";

                for (int i = 1; i < routeLegs.Count; i++)
                {
                    var leg = routeLegs[i];
                    html += $@"
                    <tr class=""route-splitting-row"">
                        <td class=""route-segment-cell"">{leg.DeparturePort?.Name ?? ""} - {leg.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(leg.Distance)} nm</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Winter:F2}</td>
                    </tr>";
                }
            }

            html += @"
                </tbody>
            </table>
        </div>";
        }
    }

    if (dataCollector.Base64String != null)
    {
        html += $@"
        <!-- Route Image Section-->
        <div class=""report-section route-image-section"">
            <h5>Route Map</h5>
            <img src=""data:image/png;base64,{dataCollector.Base64String}"" alt=""Route Image""/>
        </div>";
    }

    if (dataCollector.Notes != null &&
        (!string.IsNullOrEmpty(dataCollector.Notes.VesselCriteria) || !string.IsNullOrEmpty(dataCollector.Notes.GuideTitle)))
    {
        html += @"
        <!-- Notes Section -->
        <div class=""report-section notes-section"">
            <h5>Notes</h5>
            <ul>";

        if (!string.IsNullOrEmpty(dataCollector.Notes.VesselCriteria))
        {
            html += $@"<li>{dataCollector.Notes.VesselCriteria}</li>";
        }

        if (!string.IsNullOrEmpty(dataCollector.Notes.GuideTitle))
        {
            html += $@"<li><i>{dataCollector.Notes.GuideTitle}</i> (April 2025)</li>";
        }

        html += @"
            </ul>
        </div>";
    }

    html += @"
    </div>
</body>
</html>";

    return html;
}