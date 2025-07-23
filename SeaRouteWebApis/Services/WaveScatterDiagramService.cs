using SeaRouteWebApis.Interfaces;
using System.Text.RegularExpressions;

namespace SeaRouteWebApis.Services;

public class WaveScatterDiagramService : IWaveScatterDiagramService
{
    private readonly List<string> _heights = new();
    private readonly List<string> _periods = new();
    private readonly List<double> _occurrences = new();
    private readonly string _fileName = "composite.wsd";
    private readonly ILogger<WaveScatterDiagramService> _logger;
    private readonly string _sessionId;
    private readonly IBkWxRoutes _bkWxRoutes;

    public WaveScatterDiagramService(ILoggerFactory loggerFactory, IBkWxRoutes bkWxRoutes)
    {
        _logger = loggerFactory.CreateLogger<WaveScatterDiagramService>();
        _bkWxRoutes = bkWxRoutes;
    }

    public void CalculateReductionFactor(List<Coordinate> coordinates, string waveData, string sessionFolderPath, double exceedProb, out double targetHeight, out double reductionFactor)
    {
        try
        {


            targetHeight = 0;
            reductionFactor = 0;
            if (exceedProb <= 0 || exceedProb >= 1)
            {
                _logger.LogError("Exceedance probability should be greater than 0 and less than 1.");
                return;
            }
            ProcessWaveData(coordinates, sessionFolderPath, waveData, "Temp route", "Temp route description");

            _bkWxRoutes.ProcessWaveData(sessionFolderPath, waveData);

            // Required files
            var files = new string[] { "F101.CTL", "F101.TRA" };
            var fileContents = new Dictionary<string, string>();

            if (!ReadWsdFile(sessionFolderPath))
            {
                _logger.LogError("Error reading WSD file.");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var filePath = Path.Combine(sessionFolderPath, file);
                    if (!System.IO.File.Exists(filePath))
                    {
                        _logger.LogError($"File {file} not found.");
                        return;
                    }
                    // Ensure the file is opened with shared read access
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            fileContents[file] = reader.ReadToEnd();
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError($"Access denied to file {file}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading file {file}: {ex.Message}");
                }


            }
            double targetCumuProb = 1 - exceedProb;
            double totalOcc = _occurrences.Sum();
            double prevCumuProb = 0, currCumuProb = 0;
            int heightIndex;

            for (heightIndex = 0; heightIndex < _heights.Count; heightIndex++)
            {
                var occs = _occurrences.Skip(heightIndex * _periods.Count).Take(_periods.Count).ToList();
                double currProb = occs.Sum() / totalOcc;

                prevCumuProb = currCumuProb;
                currCumuProb += currProb;
                if (currCumuProb > targetCumuProb) break;
            }

            double prevHeight = heightIndex > 0 && double.TryParse(_heights[heightIndex - 1], out double h) ? h : 0;
            targetHeight = prevHeight + 1 / (currCumuProb - prevCumuProb) * (targetCumuProb - prevCumuProb);
            reductionFactor = Math.Min(1.08 * targetHeight / 15.46, 1);

            _logger.LogInformation($"Target wave height: {targetHeight:F8}");
            _logger.LogInformation($"Reduction factor  : {reductionFactor:F8}");
        }
        catch (Exception ex)
        {
            //if unable to calculate, returning fixed value, need to check later
            reductionFactor = 0.82;
            targetHeight = 0.0;
            _logger.LogError($"error occured on CalculateReductionFactor: {ex}");
        }
    }

    private bool IsDataValid() => _heights.Count > 0 && _periods.Count > 0 && (_heights.Count * _periods.Count) == _occurrences.Count && !_occurrences.Any(o => o < 0);


    private bool ReadWsdFile(string sessionFolderPath)
    {
        bool readError = false;
        const string tableHeaderMarker = "Hs";
        const string tableEndMarker = "---";
        bool tableStarted = false;

        string filePath = sessionFolderPath + "\\" + _fileName;

        if (!File.Exists(filePath))
        {
            _logger.LogInformation($"File not found: {filePath}");
            return false;
        }

        using StreamReader sr = new(filePath);
        string? line;
        MatchCollection matches;
        string matchedValue;
        var regex = new Regex(@"\b\S+\b", RegexOptions.Compiled);

        while ((line = sr.ReadLine()) != null)
        {
            if (line.Contains(tableHeaderMarker))
            {
                tableStarted = true;
                _periods.AddRange(regex.Matches(line).Select(m => m.Value).Where(v => !v.Contains(tableHeaderMarker)));
                continue;
            }

            if (!tableStarted)
            {
                continue;
            }

            /* Data row. Contains the wave height and its occurrences
                at each wave period. */
            matches = regex.Matches(line);
            int index = 0;
            foreach (Match match in matches)
            {
                matchedValue = match.Value;
                if (index == 0)
                {
                    _heights.Add(matchedValue);
                }
                else
                {
                    if (double.TryParse(matchedValue, out double occurrence))
                    {
                        _occurrences.Add(occurrence);
                        if (occurrence < 0)
                        {
                            readError = true;
                            string message =
                                $"Occurence value at index {index} " +
                                $"for wave height {_heights.Last()}" +
                                "is negative.";
                            _logger.LogInformation(message);
                        }
                    }
                    else
                    {
                        readError = true;
                        string message =
                            $"Occurence value at index {index} " +
                            $"for wave height {_heights.Last()}" +
                            "cannot be parsed to a number.";
                        _logger.LogInformation(message);
                    }
                }

                index++;
            }

            /* Reached table end. Stop collecting data. */
            if (line.Contains(tableEndMarker))
            {
                tableStarted = false;
                break;
            }
        } /* End of while loop */

        return !readError;
    }

    private readonly static string project = "F101";

    private void ProcessWaveData(List<Coordinate> coordinates, string sessionFolderPath, string waveData, string routeName, string routeDescription)
    {

        string traFile = Path.Combine(sessionFolderPath, $"{project}.tra");
        string ctlFile = Path.Combine(sessionFolderPath, $"{project}.ctl");

        // Write to TRA file
        using (StreamWriter sw = new StreamWriter(traFile))
        {
            sw.WriteLine("# Transit Input File");
            sw.WriteLine("# Actual service years excluding port time for entire transit routes");
            sw.WriteLine(" 20");
            sw.WriteLine("# Spectrum Type, Gamma, Cosine Spreading");
            sw.WriteLine(" 2             1             2");
            sw.WriteLine("# Bandwidth Correction (Yes=1; No=0), Wave Grid Weighting (Yes=1; No=0)");
            sw.WriteLine(" 1             1");
            sw.WriteLine("# Return Period (year)");
            sw.WriteLine(" 10");
            sw.WriteLine("# Total Number of Transit Route");
            sw.WriteLine(" 1");
            sw.WriteLine("# Name of Route");
            sw.WriteLine(routeName);
            sw.WriteLine("# Route Description");
            sw.WriteLine(routeDescription);
            sw.WriteLine("# PointNumber ServiceType, PortTimePercentage Service Year Spring Summer Fall Winter WaveDirection");
            sw.WriteLine($"{coordinates.Count}            2             0             1           1             1             1             1             3");

            sw.WriteLine("# Route Point Position");
            sw.WriteLine("# Latitude(degree) NS(1 for North, 2 for South) Longitude(degree) EW(3 for East, 4 for West)");
            foreach (var coord in coordinates)
            {
                var NS = coord.Latitude < 0 ? 2 : 1;
                var EW = coord.Longitude > 0 ? 3 : 4;
                sw.WriteLine($" {coord.Latitude}          {NS}             {coord.Longitude}       {EW}");
            }

            //here changes 
            foreach (var coord in coordinates)
            {
                // Apply the formulas exactly as in the Excel spreadsheet:
                // For longitude (A column in Excel): =IF(ABS(A2)>179.99,179.99,ABS(A2))
                double absLongitude = Math.Min(Math.Abs(coord.Longitude), 179.99);

                // For latitude (B column in Excel): =ABS(B2)
                double absLatitude = Math.Abs(coord.Latitude);

                // For East/West indicator (G column in Excel): = IF(A2 >= 0, 3, 4)
                // 3 means East, 4 means West
                var EW = coord.Longitude >= 0 ? 3 : 4;

                // For North/South indicator (E column in Excel): = IF(B2 >= 0, 1, 2)
                // 1 means North, 2 means South
                var NS = coord.Latitude >= 0 ? 1 : 2;

                // Format with consistent spacing and write absolute values
                sw.WriteLine($" {absLatitude,-15:F6} {NS,-12} {absLongitude,-15:F6} {EW}");
            }
        }

        // Write to CTL file


        using (StreamWriter sw = new StreamWriter(ctlFile))
        {
            sw.WriteLine("# Project Control File");
            sw.WriteLine("# Project Name");
            sw.WriteLine(project);
            sw.WriteLine("# Vessel Length - Lbp (m), Speed (knots), Service Age (year)");
            sw.WriteLine("245           0             0");
            sw.WriteLine("# FLGT Type (New=1; Conversion=3)");
            sw.WriteLine("1");
            sw.WriteLine("# Analysis Type (Fatigue only=0; Strength only=1; Fatigue+Strength=2)");
            sw.WriteLine("2");
            sw.WriteLine("# Intend_Site, Transit, Historical_Site, Historical_Route (Yes=1; No=0)");
            sw.WriteLine("0             1             0             0");
            sw.WriteLine("# System Directory");
            sw.WriteLine(".");
            sw.WriteLine("# Working Directory");
            sw.WriteLine(".");
            sw.WriteLine("# RAO Type (User-defined=1; System Tanker=2; System FLGT=3; Seakeeping RAO=4)");
            sw.WriteLine("3");
            sw.WriteLine("# Path of Project Control File");
            sw.WriteLine($".\\{project}.CTL");
            sw.WriteLine("# WAVETYPE (ABS=ABSWAVE, BMT=BMTWAVE)");
            sw.WriteLine(waveData);
        }



        _logger.LogInformation($"Files created successfully in temp directory: {sessionFolderPath}");
    }
}
