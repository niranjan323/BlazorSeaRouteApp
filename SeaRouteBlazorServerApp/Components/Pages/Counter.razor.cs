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
            background-color: #0066cc;
            color: white;
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
        /* CLP-V specific styles */
        .clpv-reduction-factor {{
            font-size: 16px;
            margin-bottom: 20px;
        }}
        .route_content {{
            margin-top: 20px;
        }}
        .cls_route_div {{
            width: 100%;
        }}
        .cls_route_table {{
            width: 100%;
        }}
        .cls_route_table table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
        }}
        .cls_route_table td {{
            border: 1px solid #ddd;
            padding: 8px;
            text-align: center;
            vertical-align: middle;
        }}
        .cls_table_div_outline {{
            display: flex;
            width: 100%;
        }}
        .cls_table_div {{
            padding: 8px;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            text-align: center;
        }}
        .cls_width33 {{
            width: 33.33%;
        }}
        .cls_width25 {{
            width: 25%;
        }}
        .cls_table_img {{
            max-width: 30px;
            max-height: 30px;
            margin-bottom: 5px;
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
            <p class=""download-timestamp""><strong>Downloaded:</strong> {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</p>
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

    if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.Flag))
    {
        html += $@"
                <tr>
                    <td><strong>Flag:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.Flag}</td>
                    <td></td>
                </tr>";
    }

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

    // Output Section - Conditional rendering based on isCLPVChecked
    if (dataCollector.ReductionFactorResults != null && dataCollector.ReductionFactorResults.Any())
    {
        html += @"
        <!-- Output Section -->
        <div class=""report-section"">
            <h5>Output</h5>";

        if (isCLPVChecked)
        {
            // Simple reduction factor display for CLP-V
            var entireRoute = dataCollector.ReductionFactorResults.FirstOrDefault(r => r.VoyageLegOrder == 0);
            if (entireRoute != null)
            {
                // Apply the max logic: max(reductionFactor, 0.80)
                decimal reductionFactor = Math.Max((decimal)entireRoute.ReductionFactors.Annual, 0.80m);

                html += $@"
            <p class=""clpv-reduction-factor"">
                <strong>Reduction Factor: </strong>
                <span class=""reduction-factor"">{Math.Round(reductionFactor, 2):F2}</span>
            </p>";
            }
        }
        else
        {
            // Full detailed table for CLP-V(PARR) - existing implementation
            html += @"
            <div class=""route_content"">
                <div class=""cls_route_div"">
                    <div class=""cls_route_table"">
                        <table>
                            <tbody>
                                <tr>
                                    <td rowspan=""2""></td>
                                    <td colspan=""3""></td>
                                    <td colspan=""4"">
                                        <img src=""img/group-3805-1@1x.png"" alt=""Seasonal Reduction Factor"" class=""cls_table_img"" />
                                        <span style=""font-size: 12px;""> Seasonal Reduction Factor </span>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan=""3"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width33"">
                                                <img src=""img/group-3812-1@1x.png"" alt=""Routes"" class=""cls_table_img"" />
                                                Routes
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                <img src=""img/group-3814-2@1x.png"" alt=""Distance"" class=""cls_table_img"" />
                                                Distance
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                <img src=""img/group-3832-1@1x.png"" alt=""Annual Reduction Factor"" class=""cls_table_img"" />
                                                Annual Reduction Factor
                                            </div>
                                        </div>
                                    </td>
                                    <td colspan=""4"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width25"">
                                                <img src=""img/spring.png"" alt=""Spring"" class=""cls_table_img"" />
                                                Spring
                                                <p>(Mar-May)</p>
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                <img src=""img/summer.png"" alt=""Summer"" class=""cls_table_img"" />
                                                Summer
                                                <p>(Jun-Aug)</p>
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                <img src=""img/fall.png"" alt=""Fall"" class=""cls_table_img"" />
                                                Fall
                                                <p>(Sep-Nov)</p>
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                <img src=""img/winter.png"" alt=""Winter"" class=""cls_table_img"" />
                                                Winter
                                                <p>(Dec-Feb)</p>
                                            </div>
                                        </div>
                                    </td>
                                </tr>";

            var entireRoute = dataCollector.ReductionFactorResults.FirstOrDefault(r => r.VoyageLegOrder == 0);
            if (entireRoute != null)
            {
                html += $@"
                                <tr>
                                    <td><span style=""font-size: 12px;"">Entire Route</span></td>
                                    <td colspan=""3"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width33"">
                                                {entireRoute.DeparturePort?.Name ?? ""} - {entireRoute.ArrivalPort?.Name ?? ""}
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                {Math.Round(entireRoute.Distance)} nm
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                {entireRoute.ReductionFactors.Annual:F2}
                                            </div>
                                        </div>
                                    </td>
                                    <td colspan=""4"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width25"">
                                                {entireRoute.ReductionFactors.Spring:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {entireRoute.ReductionFactors.Summer:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {entireRoute.ReductionFactors.Fall:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {entireRoute.ReductionFactors.Winter:F2}
                                            </div>
                                        </div>
                                    </td>
                                </tr>";
            }

            var routeLegs = dataCollector.ReductionFactorResults.Where(r => r.VoyageLegOrder > 0).OrderBy(r => r.VoyageLegOrder).ToList();
            foreach (var leg in routeLegs)
            {
                html += $@"
                                <tr>
                                    <td><span style=""font-size: 12px;"">Route Splitting</span></td>
                                    <td colspan=""3"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width33"">
                                                {leg.DeparturePort?.Name ?? ""} - {leg.ArrivalPort?.Name ?? ""}
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                {Math.Round(leg.Distance)} nm
                                            </div>
                                            <div class=""cls_table_div cls_width33"">
                                                {leg.ReductionFactors.Annual:F2}
                                            </div>
                                        </div>
                                    </td>
                                    <td colspan=""4"">
                                        <div class=""cls_table_div_outline"">
                                            <div class=""cls_table_div cls_width25"">
                                                {leg.ReductionFactors.Spring:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {leg.ReductionFactors.Summer:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {leg.ReductionFactors.Fall:F2}
                                            </div>
                                            <div class=""cls_table_div cls_width25"">
                                                {leg.ReductionFactors.Winter:F2}
                                            </div>
                                        </div>
                                    </td>
                                </tr>";
            }

            html += @"
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>";
        }

        html += @"
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