namespace SeaRouteWebApis.Services;
using System.Security.AccessControl;
using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using SeaRouteWebApis.Interfaces;

public class BkWxRoutes : IBkWxRoutes
{
    private readonly ILogger<BkWxRoutes> _logger;
    private readonly string _sessionId;
    private readonly string _tempFolderPath;
    public BkWxRoutes(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BkWxRoutes>();

    }
    #region private variables      
    // private  readonly string AppPath = Path.GetTempPath();      
    public static string AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    string FF190 = "Composite Wave Grid Result: {0}: {1}";


    string PROJNAME, SAVEFLG, WOKDIR, CTLFILE, WAVETYPE, SYSDIR, FILENAME, FILE12, FILE15, FILE46, FILE19;
    string BLP, VESSEL_SPEED, VESSEL_AGE, IRAOTYPE, CPATH;

    bool EXISTS;

    int IANALYSIS_TYPE, IVESSEL_TYPE, IFLAG_ISTE, IFLAG_TRAN, IFLAG_HSTE, IFLAG_HRTE;
    int NSITE, ISITE, NPOINT, IPOINT, MSITE, IDUM, MPTS, ID_WSD, NWSD;

    public static int NGRID = 1102;
    public static int NHEADING_FLAG = 6;
    public static int NDIR = 24;
    public static int NHGT = 20;
    public static int NPER = 17;
    public static float HGTB = 0.5f;
    public static float DHGT = 1.0f;
    public static float PERB = 3.5f;
    public static float DPER = 1.0f;

    public static float[] HS = new float[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f,
                                                  10.5f, 11.5f, 12.5f, 13.5f, 14.5f, 15.5f, 16.5f, 17.5f, 18.5f, 19.5f };
    public static float[] TZ = new float[] { 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f, 10.5f, 11.5f, 12.5f,
                                                  13.5f, 14.5f, 15.5f, 16.5f, 17.5f, 18.5f, 19.5f };


    string[] VALUES;
    string[] LINE_VALUES;

    int[] ID = new int[NGRID];
    int[] IHEADING_FLAG = new int[NHEADING_FLAG];
    int[] NPTS = new int[200], ID_WSD_ROUTE = new int[200];
    int[,] NOCCUR = new int[NHGT, NPER];
    int[] HMARGIN = new int[NHGT];
    int[] TMARGIN = new int[NPER];
    int[] TOTAL = new int[NGRID];

    float[,] LATI = new float[2000, 2];
    float[,] LONG = new float[2000, 2];

    float[] WAVEP = new float[NDIR + 1], WINDP = new float[NDIR + 1];
    float[] WAVEP1 = new float[NDIR + 1], WINDP1 = new float[NDIR + 1];
    float[] WAVEP_VESSEL_READ = new float[NDIR];
    float[] HS_WRITE = new float[NHGT * NPER];
    float[] TZ_WRITE = new float[NHGT * NPER];
    float[] XINT = new float[500];
    float[] YINT = new float[500];
    float[] XLOC = new float[500];
    float[] YLOC = new float[500];
    float[] XINT_NEW = new float[500];
    float[] YINT_NEW = new float[500];
    double[,] COMPOSITEWSDA = new double[NHGT, NPER];

    int[] NOCCUR_WRITE = new int[NHGT * NPER];

    int NPT, INBOUND, IFLAG_WAVEDIR, IROUTE, NSEASTATES, NROUTES, NRTS_TRA, NRTS_RTE, IHGT, IPER;
    int ISPRING, ISUMMER, IWINTER, IFALL, INTSECT, IZERO = 0;
    int NDIR1, I_LATI, I_LONG, ISPECT1, MSPREAD1, NDIR24, I_CONER, N, ID_SITE_NEIGHBOR;
    int IBAND_READ, IUSER_WSD_READ, ID_WSD_READ, NSEASTATES_READ, NPEAK_READ, IPERIOD_READ;
    int ISPECT1_READ, MSPREAD1_READ, NOCCUR_READ;
    int IDIR = 0, NDIR_READ, IUSER_WSD, IBAND, NPEAK, IWEIGHTING_GRID, NRTS, IROUTE_READ, IWSD, NWSD_READ, NWSD_TOTAL_TRANSIT;
    int ID_SITE_LAST = -1, JUMP, IWRITE_ONCE, ID_SITE, ID_SITE1, ID_SITE2, I_LATI_DUM, I_LONG_DUM;

    string ROUTE_NAME, ROUTE_DESCRIPTION, ROUTE_NAME_PRINT, ROUTE_TYPE;
    string TEMP51, TEMP52, TEMP53;

    float I_LATI1, I_LATI2, I_LONG1, I_LONG2, TOTALYEARS, TIMEIDLE_PERCENTAGE, YEAR, TOT_YEAR;
    float SITE_LATI, SITE_LONG;

    float Y0, WEIGHTING_READ, COMPASS_ANGLE_READ, COMPASS_ANGLE = 0.0f;
    float TZ_READ, GAMMA1_READ, HS_READ, SUM, THETA, BETA;
    float RETURN_PERIOD, EXPOSURE_WEIGHTING, GAMMA1;
    float RETURN_PERIOD_READ, YEAR_READ, TIMEIDLE_PERCENTAGE_READ;
    float pi = (float)Math.Acos(-1.0);
    float A1, B1, A2, B2, D, DISTANCE_LIMIT, DISTANCE_KM, MPT, DL, COURSE_ANGLE, XEND, YEND;
    float DISTANCE_INTERMEDIATE, F, A, B, X, Y, Z, YNEW, XNEW, X1, X2, Y1, Y2, DET, UA, UB, XPOS1, YPOS1, XPOS2, YPOS2;
    float X3, Y3, X4, Y4, PL, XT, YT, PROBSUMA;
    ushort JUNK;

    private ABSStreamWriter FSW46; //.ROS
    private ABSStreamWriter FSW39; //TEMP39
    private ABSStreamWriter FSW53; //TEMP53.tmp
    private ABSStreamWriter FSW77; //composite.sct
    private ABSStreamWriter FSW81; //summary.txt
    private ABSStreamWriter FSW83; //detail.txt
    private ABSStreamWriter FSW29; //composite.wsd
    private ABSStreamWriter FSW19; //intermediate.wsd
    private ABSStreamWriter FSW777;//check.dat

    private ABSStreamReader FSR25; //.ist or .tra or .hst or .rte
    private ABSStreamReader FSR39; //TEMP39
    private ABSStreamReader FSR15; //abswave.key or bmtwave.key
    private ABSStreamReader FSR53; //TEMP53.tmp
    private ABSStreamReader FSR35; //intermediate.wsd

    private BinaryWriter FSBW26;
    private BinaryReader FSBR15;

    #endregion
  
    public void ProcessWaveData(string sessionFolderPath, string waveData, string seasonType = "annual")
    {
        // Validate seasonType
        string[] validSeasons = { "annual", "spring", "summer", "fall", "winter" };
        if (!validSeasons.Contains(seasonType.ToLower()))
        {
            _logger.LogWarning($"Invalid seasonType '{seasonType}' provided. Defaulting to 'annual'.");
            seasonType = "annual";
        }
        // Define file paths within the session folder
        string traFilePath = Path.Combine(sessionFolderPath, "F101.tra");
        string ctlFilePath = Path.Combine(sessionFolderPath, "F101.ctl");
        try
        {
            // Read files
            if (File.Exists(ctlFilePath))
            {
                using (FSW777 = FileIO.GetFileStreamWriter("check.dat", FileMode.Create))
                {
                    FILE12 = ctlFilePath.Trim();
                    EXISTS = File.Exists(FILE12);
                    if (!EXISTS)
                    {
                        Console.WriteLine($">> Cannot find CTL file {FILE12}");
                        //Environment.Exit(1);
                    }
                    ReadCtlFile(ctlFilePath);
                    // Open file
                    if (WAVETYPE == "ABS")
                    {
                        NGRID = 1102;
                        NHGT = 20;
                        NPER = 17;
                        NDIR = 24;
                        HGTB = 0.5f;
                        DHGT = 1.0f;
                        PERB = 3.5f;
                        DPER = 1.0f;
                    }
                    else if (WAVETYPE == "BMT")
                    {
                        NGRID = 117;
                        NHGT = 15;
                        NPER = 11;
                        NDIR = 8;
                        HGTB = 0.5f;
                        DHGT = 1.0f;
                        PERB = 3.5f;
                        DPER = 1.0f;
                    }
                    else
                    {
                        Console.WriteLine("WAVETYPE [" + waveData + "] is not recognized. Check the input file!");
                        //Environment.Exit(0);
                    }
                    for (int i = 0; i < NHGT; i++)
                    {
                        HS[i] = HGTB + (i * DHGT);
                    }
                    for (int i = 0; i < NPER; i++)
                    {
                        TZ[i] = PERB + (i * DPER);
                    }
                    SYSDIR = sessionFolderPath + "\\";
                    WOKDIR = sessionFolderPath + "\\";
                    FILENAME = WOKDIR.Trim() + PROJNAME;
                    FILE46 = FILENAME + ".ROS"; // for view rosette & weighting
                    using (FSW46 = FileIO.GetFileStreamWriter(FILE46, FileMode.Create))
                    {
                        using (FSW39 = FileIO.GetFileStreamWriter("TEMP39", FileMode.Create))
                        {
                            FILE19 = WOKDIR.Trim() + "intermediate.wsd";
                            using (FSW19 = FileIO.GetFileStreamWriter(FILE19, FileMode.Create))
                            {
                                // Logic for handling routes based on flags
                                if (IFLAG_ISTE == 1 && SAVEFLG[0] == '1')
                                    Wxroute(1, FILENAME, IANALYSIS_TYPE, seasonType);
                                if (IFLAG_TRAN == 1 && SAVEFLG[1] == '1')
                                    Wxroute(2, FILENAME, IANALYSIS_TYPE, seasonType);
                                if (IFLAG_HSTE == 1 && SAVEFLG[2] == '1')
                                    Wxroute(3, FILENAME, IANALYSIS_TYPE, seasonType);
                                if (IFLAG_HRTE == 1 && SAVEFLG[3] == '1')
                                    Wxroute(4, FILENAME, IANALYSIS_TYPE, seasonType);
                            }
                        }
                        PRintWSDS(WOKDIR);
                    }
                    Console.WriteLine("Program successfully completed ......");
                }
            }
        }

        catch (NullReferenceException ex)
        {
            Console.WriteLine($"NullReferenceException: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            throw;
        }
    }



    public void ReadCtlFile(string filePath)
    {
        using (var FSR12 = FileIO.GetFileStreamReader(FILE12, FileMode.Open))
        {
            FSR12.ReadLine();
            FSR12.ReadLine();
            PROJNAME = FSR12.ReadLine();
            FSR12.ReadLine();
            LINE_VALUES = FileIO.Read(FSR12);
            BLP = LINE_VALUES[0];
            VESSEL_SPEED = LINE_VALUES[1];
            VESSEL_AGE = LINE_VALUES[2];
            FSR12.ReadLine();
            IVESSEL_TYPE = int.Parse(FSR12.ReadLine().Trim());
            FSR12.ReadLine();
            IANALYSIS_TYPE = int.Parse(FSR12.ReadLine().Trim());
            FSR12.ReadLine();
            LINE_VALUES = FileIO.Read(FSR12);
            IFLAG_ISTE = int.Parse(LINE_VALUES[0].Trim());
            IFLAG_TRAN = int.Parse(LINE_VALUES[1].Trim());
            IFLAG_HSTE = int.Parse(LINE_VALUES[2].Trim());
            IFLAG_HRTE = int.Parse(LINE_VALUES[3].Trim());
            SAVEFLG = String.Concat(Convert.ToString(IFLAG_ISTE), Convert.ToString(IFLAG_TRAN), Convert.ToString(IFLAG_HSTE), Convert.ToString(IFLAG_HRTE));
            FSR12.ReadLine();
            SYSDIR = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            WOKDIR = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            IRAOTYPE = FSR12.ReadLine();
            FSR12.ReadLine();
            CPATH = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            WAVETYPE = FSR12.ReadLine().Trim();
        }
    }

    private static string[] ReadValues(StreamReader sr, string delimeter = " ")
    {
        var line = sr.ReadLine();
        var values = line.Split(new[] { delimeter }, StringSplitOptions.RemoveEmptyEntries);

        return values;
    }

    private void ProcessCTLFile(string CTLFILE)
    {
        using (FSW777 = FileIO.GetFileStreamWriter("check.dat", FileMode.Create))
        {
            FILE12 = CTLFILE.Trim();
            EXISTS = File.Exists(FILE12);

            if (!EXISTS)
            {
                Console.WriteLine($">> Cannot find CTL file {FILE12}");
                //Environment.Exit(1);
            }

            ReadControlFile();

            SetWaveTypeProperties();

            SYSDIR = AppPath + "\\";
            WOKDIR = AppPath + "\\";

            FILENAME = WOKDIR.Trim() + PROJNAME;
            FILE46 = FILENAME + ".ROS"; // For view rosette & weighting

            using (FSW46 = FileIO.GetFileStreamWriter(FILE46, FileMode.Create))
            {
                using (FSW39 = FileIO.GetFileStreamWriter("TEMP39", FileMode.Create))
                {
                    FILE19 = WOKDIR.Trim() + "intermediate.wsd";
                    using (FSW19 = FileIO.GetFileStreamWriter(FILE19, FileMode.Create))
                    {
                        ProcessRoutes();
                    }
                }
                PRintWSDS(WOKDIR);
            }
            Console.WriteLine("Program successfully completed ......");
        }
    }

    private void ReadControlFile()
    {
        using (var FSR12 = FileIO.GetFileStreamReader(FILE12, FileMode.Open))
        {
            FSR12.ReadLine();
            FSR12.ReadLine();
            PROJNAME = FSR12.ReadLine();
            FSR12.ReadLine();
            LINE_VALUES = FileIO.Read(FSR12);
            BLP = LINE_VALUES[0];
            VESSEL_SPEED = LINE_VALUES[1];
            VESSEL_AGE = LINE_VALUES[2];
            FSR12.ReadLine();
            IVESSEL_TYPE = int.Parse(FSR12.ReadLine().Trim());
            FSR12.ReadLine();
            IANALYSIS_TYPE = int.Parse(FSR12.ReadLine().Trim());
            FSR12.ReadLine();
            LINE_VALUES = FileIO.Read(FSR12);
            IFLAG_ISTE = int.Parse(LINE_VALUES[0]);
            IFLAG_TRAN = int.Parse(LINE_VALUES[1]);
            IFLAG_HSTE = int.Parse(LINE_VALUES[2]);
            IFLAG_HRTE = int.Parse(LINE_VALUES[3]);
            SAVEFLG = $"{IFLAG_ISTE}{IFLAG_TRAN}{IFLAG_HSTE}{IFLAG_HRTE}";
            FSR12.ReadLine();
            SYSDIR = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            WOKDIR = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            IRAOTYPE = FSR12.ReadLine();
            FSR12.ReadLine();
            CPATH = FSR12.ReadLine().Trim();
            FSR12.ReadLine();
            WAVETYPE = FSR12.ReadLine().Trim();
        }
    }

    private void SetWaveTypeProperties()
    {
        if (WAVETYPE == "ABS")
        {
            NGRID = 1102; NHGT = 20; NPER = 17; NDIR = 24;
            HGTB = 0.5f; DHGT = 1.0f; PERB = 3.5f; DPER = 1.0f;
        }
        else if (WAVETYPE == "BMT")
        {
            NGRID = 117; NHGT = 15; NPER = 11; NDIR = 8;
            HGTB = 0.5f; DHGT = 1.0f; PERB = 3.5f; DPER = 1.0f;
        }
        else
        {
            Console.WriteLine($"WAVETYPE [{WAVETYPE}] is not recognized. Check the input file!");
            //Environment.Exit(0);
        }

        for (int i = 0; i < NHGT; i++) HS[i] = HGTB + (i * DHGT);
        for (int i = 0; i < NPER; i++) TZ[i] = PERB + (i * DPER);
    }

    private void ProcessRoutes(string seasonType)
    {
        if (IFLAG_ISTE == 1 && SAVEFLG[0] == '1') Wxroute(1, FILENAME, IANALYSIS_TYPE, seasonType);
        if (IFLAG_TRAN == 1 && SAVEFLG[1] == '1') Wxroute(2, FILENAME, IANALYSIS_TYPE, seasonType);
        if (IFLAG_HSTE == 1 && SAVEFLG[2] == '1') Wxroute(3, FILENAME, IANALYSIS_TYPE, seasonType);
        if (IFLAG_HRTE == 1 && SAVEFLG[3] == '1') Wxroute(4, FILENAME, IANALYSIS_TYPE, seasonType);
    }

    private void Wxroute(int IFIELD, string FILENAME, int IANALYSIS_TYPE, string seasonType)
    {
        string FILE25 = GetFilePath(IFIELD, FILENAME);
        if (!File.Exists(FILE25))
        {
            Console.WriteLine($">> Cannot find file {FILE25}");
            //Environment.Exit(1);
        }

        try
        {
            using (FSR25 = FileIO.GetFileStreamReader(FILE25, FileMode.Open, FileShare.ReadWrite, FileAccess.ReadWrite))
            {
                string FILE26 = GetOutputFilePath(IFIELD, FILENAME);
                using (FSBW26 = FileIO.GetBinaryFileStreamWriter(FILE26, FileMode.Create))
                {
                    ProcessFile(IFIELD, IANALYSIS_TYPE, seasonType);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing Wxroute: {ex.Message}");
        }
    }

    private string GetFilePath(int IFIELD, string FILENAME)
    {
        Console.WriteLine(IFIELD == 1 ? "Processing for Intended Site ........"
                          : IFIELD == 2 ? "Processing for Transit Condition ...."
                          : IFIELD == 3 ? "Processing for Historical Site ......"
                          : "Processing for Historical Route .....");

        return FILENAME.Trim() + (IFIELD == 1 ? ".ist"
                                   : IFIELD == 2 ? ".tra"
                                   : IFIELD == 3 ? ".hst"
                                   : ".rte");
    }

    private string GetOutputFilePath(int IFIELD, string FILENAME)
    {
        return FILENAME.Trim() + (IFIELD == 1 ? ".wd1"
                                   : IFIELD == 2 ? ".wd2"
                                   : IFIELD == 3 ? ".wd3"
                                   : ".wd4");
    }

    private void ProcessFile(int IFIELD, int IANALYSIS_TYPE, string seasonType)
    {
        string DUMMYLINE = FSR25.ReadLine();
        FSBW26.Write(DUMMYLINE.ToArray());

        if (IFIELD == 1 || IFIELD == 3)
        {
            ProcessSiteData(IFIELD, IANALYSIS_TYPE, seasonType);
        }
        else if (IFIELD == 2 || IFIELD == 4)
        {
            ProcessRouteData(IFIELD, IANALYSIS_TYPE, seasonType);
        }
    }

    private void ProcessSiteData(int IFIELD, int IANALYSIS_TYPE, string seasonType)
    {
        float ZERO = 0.0f;
        FSBW26.Write(JUNK);
        FSBW26.Write(ZERO);

        FileIO.Read(FSR25);
        NSITE = int.Parse(FileIO.Read(FSR25)[0]);
        FSBW26.Write(NSITE);
        FSBW26.Write(JUNK);
        FSBW26.Write(1.0f);

        for (ISITE = 0; ISITE < NSITE; ISITE++)
        {
            // WXSITE(IFIELD, IANALYSIS_TYPE, ISITE, NSITE, seasonType);
        }
    }

    private void ProcessRouteData(int IFIELD, int IANALYSIS_TYPE, string seasonType)
    {
        if (IFIELD == 2) NWSD_TOTAL_TRANSIT = 0;
        WXROUT(IFIELD, IANALYSIS_TYPE, seasonType);
    }

    private void WXROUT(int IFIELD, int IANALYSIS_TYPE, string seasonType)
    {
        if (WAVETYPE == "ABS")
            FILE15 = Path.Combine(SYSDIR.Trim(), "abswave.key");
        else if (WAVETYPE == "BMT")
            FILE15 = Path.Combine(SYSDIR.Trim(), "bmtwave.key");

        using (FSR15 = FileIO.GetFileStreamReader(FILE15, FileMode.Open))
        {

            for (int i = 0; i < NGRID; i++)
            {
                VALUES = FileIO.Read(FSR15);
                ID[i] = int.Parse(VALUES[0]);

                LATI[ID[i], 0] = float.Parse(VALUES[1]);
                I_LATI1 = float.Parse(VALUES[2]);
                LONG[ID[i], 0] = float.Parse(VALUES[3]);
                I_LONG1 = float.Parse(VALUES[4]);
                LATI[ID[i], 1] = float.Parse(VALUES[5]);
                I_LATI2 = float.Parse(VALUES[6]);
                LONG[ID[i], 1] = float.Parse(VALUES[7]);
                I_LONG2 = float.Parse(VALUES[8]);

                if (I_LATI1 == 2) LATI[ID[i], 0] = -LATI[ID[i], 0];
                if (I_LATI2 == 2) LATI[ID[i], 1] = -LATI[ID[i], 1];
                if (I_LONG1 == 4) LONG[ID[i], 0] = -LONG[ID[i], 0];
                if (I_LONG2 == 4) LONG[ID[i], 1] = -LONG[ID[i], 1];
            }
        }
        for (int i = 0; i < NGRID; i++)
        {
            FileIO.WriteLine(FSW777, "{0,10} {1,12:0.00} {1,12:0.00} {1,12:0.00} {1,12:0.00}", ID[i], LATI[ID[i] - 1, 0], LONG[ID[i] - 1, 0], LATI[ID[i] - 1, 1], LONG[ID[i] - 1, 1]);
        }

        IZERO = 0;
        FileIO.Read(FSR25);
        TOTALYEARS = float.Parse(FileIO.ReadLine(FSR25));
        FSBW26.Write(JUNK);
        FSBW26.Write(TOTALYEARS);
        TOT_YEAR = 0.0f;
        if (IHEADING_FLAG[NHEADING_FLAG - 2] != 0)
        {
            Console.WriteLine("!!!" + IHEADING_FLAG[NHEADING_FLAG - 2] + "should be 0. Please check this data.");
        }
        FileIO.Read(FSR25);
        LINE_VALUES = FileIO.Read(FSR25);
        ISPECT1 = int.Parse(LINE_VALUES[0]);
        GAMMA1 = float.Parse(LINE_VALUES[1]);
        MSPREAD1 = int.Parse(LINE_VALUES[2]);
        FileIO.Read(FSR25);
        if (IFIELD == 2)
        {
            LINE_VALUES = FileIO.Read(FSR25);
            IBAND = int.Parse(LINE_VALUES[0]);
            IWEIGHTING_GRID = int.Parse(LINE_VALUES[1]);
        }
        else
        {
            IBAND = int.Parse(FileIO.ReadLine(FSR25));
            IWEIGHTING_GRID = 1;
        }
        if (IFIELD == 2)
        {
            FileIO.Read(FSR25);
            RETURN_PERIOD = float.Parse(FileIO.ReadLine(FSR25));
        }
        FileIO.Read(FSR25);
        NRTS = int.Parse(FileIO.ReadLine(FSR25));    // NRTS=TOTAL NUMBER OF ROUTES

        if (IFIELD == 2) NRTS_TRA = NRTS;

        if (IFIELD == 4) NRTS_RTE = NRTS;

        TEMP53 = SYSDIR + "TEMP53.tmp";
        WxRouteFileOperations(IFIELD, IANALYSIS_TYPE, seasonType);


    }

    private void WxRouteFileOperations(int IFIELD, int IANALYSIS_TYPE, string seasonType)
    {
        using (FSW53 = FileIO.GetFileStreamWriter(TEMP53, FileMode.Create))//new BinaryWriter(ms))
        {
            FileIO.WriteLine(FSW53, "{0}", NRTS);
            for (IROUTE = 0; IROUTE < NRTS; IROUTE++)
            {
                Console.WriteLine($"+ iroute, nrts= {IROUTE}, {NRTS}");

                FileIO.Read(FSR25);

                ROUTE_NAME = FileIO.Read(FSR25)[0];

                FileIO.WriteLine(FSW53, "{0} {1}", ROUTE_NAME, IROUTE + 1);
                FileIO.WriteLine(FSW19, "{0}", ROUTE_NAME);
                FileIO.Read(FSR25);
                ROUTE_DESCRIPTION = FileIO.ReadLine(FSR25);

                FileIO.WriteLine(FSW53, "{0} {1}", ROUTE_DESCRIPTION, IROUTE + 1);

                FileIO.Read(FSR25);

                LINE_VALUES = FileIO.Read(FSR25);
                NPT = int.Parse(LINE_VALUES[0]);
                INBOUND = int.Parse(LINE_VALUES[1]);
                TIMEIDLE_PERCENTAGE = float.Parse(LINE_VALUES[2]);
                YEAR = float.Parse(LINE_VALUES[3]);
                ISPRING = int.Parse(LINE_VALUES[4]);
                ISUMMER = int.Parse(LINE_VALUES[5]);
                IWINTER = int.Parse(LINE_VALUES[6]);
                IFALL = int.Parse(LINE_VALUES[7]);
                IFLAG_WAVEDIR = int.Parse(LINE_VALUES[8]);

                TOT_YEAR = TOT_YEAR + YEAR * (1.0f - TIMEIDLE_PERCENTAGE);
                RouteWSDs(IFIELD, IROUTE, ROUTE_NAME, NPT, INBOUND, TIMEIDLE_PERCENTAGE, RETURN_PERIOD, YEAR, IFLAG_WAVEDIR, IBAND, IWEIGHTING_GRID, seasonType);

            }
            FSW53.Seek(0);
        }

        int IONE;
        using (FSR53 = FileIO.GetFileStreamReader(TEMP53, FileMode.Open))
        {
            NRTS = int.Parse(FileIO.Read(FSR53)[0]);

            if (IFIELD == 2 && IROUTE != 0)
            {
                IONE = 1;
                FSBW26.Write((ushort)IONE);
                FSBW26.Write(IWEIGHTING_GRID);
            }
            else
            {
                FSBW26.Write((ushort)NRTS);
                FSBW26.Write(IWEIGHTING_GRID);
            }
            for (IROUTE = 0; IROUTE < NRTS; IROUTE++)
            {
                LINE_VALUES = FileIO.Read(FSR53);
                ROUTE_NAME = LINE_VALUES[0];
                IROUTE_READ = int.Parse(LINE_VALUES[1]);

                LINE_VALUES = FileIO.Read(FSR53);
                ROUTE_DESCRIPTION = "";
                for (int x = 0; x < LINE_VALUES.Count() - 1; x++)
                    ROUTE_DESCRIPTION = ROUTE_DESCRIPTION + " " + LINE_VALUES[x];
                IROUTE_READ = int.Parse(LINE_VALUES[LINE_VALUES.Count() - 1]);

                NWSD_READ = int.Parse(FileIO.Read(FSR53)[0]);

                LINE_VALUES = FileIO.Read(FSR53);
                IROUTE_READ = int.Parse(LINE_VALUES[0]);
                RETURN_PERIOD_READ = float.Parse(LINE_VALUES[1]);
                YEAR_READ = float.Parse(LINE_VALUES[2]);
                TIMEIDLE_PERCENTAGE_READ = float.Parse(LINE_VALUES[3]);

                for (int i = 0; i < NHEADING_FLAG; i++)
                {
                    IHEADING_FLAG[i] = int.Parse(FileIO.Read(FSR53)[0]);
                }

                NDIR24 = 24;
                if (IHEADING_FLAG[NHEADING_FLAG - 1] != 0)
                {
                    if (IFIELD == 2) ROUTE_NAME_PRINT = ROUTE_NAME.Trim(); // ' : TRANSIT'
                    if (IFIELD == 4) ROUTE_NAME_PRINT = ROUTE_NAME.Trim(); // ' : HISTORICAL ROUTE'
                    FileIO.WriteLine(FSW46, ROUTE_NAME);
                    FileIO.WriteLine(FSW46, ROUTE_DESCRIPTION);
                    FileIO.WriteLine(FSW46, " {0,3} {1,3}", NWSD_READ, NDIR24);
                }
                FSW39.Write("{0,4} {1}", NWSD_READ, ROUTE_NAME);
                if (IFIELD == 2 && IROUTE == 0)
                {
                    ROUTE_NAME = "TRANSIT";
                    ROUTE_DESCRIPTION = "TRANSIT ROUTES";
                }
                if (IFIELD == 2 && IROUTE != 0)
                {//skip write - same as Fortran
                }

                else
                {
                    FSBW26.Write(ROUTE_NAME.ToArray());
                    FSBW26.Write((ushort)IROUTE_READ + 1);
                    FSBW26.Write(ROUTE_DESCRIPTION.ToArray());
                    FSBW26.Write((ushort)IROUTE_READ + 1);
                    if (IFIELD == 2)
                    {
                        FSBW26.Write((ushort)NWSD_TOTAL_TRANSIT);
                        FSBW26.Write((ushort)IROUTE_READ + 1);
                        FSBW26.Write(JUNK);
                        FSBW26.Write(RETURN_PERIOD_READ);
                        FSBW26.Write(TOT_YEAR);
                        FSBW26.Write(0.0f);
                    }
                    else
                    {
                        FSBW26.Write((ushort)NWSD_READ);
                        FSBW26.Write((ushort)IROUTE_READ);
                        FSBW26.Write(JUNK);
                        FSBW26.Write(RETURN_PERIOD_READ);
                        FSBW26.Write(YEAR_READ);
                        FSBW26.Write(TIMEIDLE_PERCENTAGE_READ);
                    }
                    for (int i = 0; i < NHEADING_FLAG; i++)
                    {
                        FSBW26.Write(IHEADING_FLAG[i]);
                    }
                }


                for (IWSD = 0; IWSD < NWSD_READ; IWSD++) //1035
                {
                    if (IHEADING_FLAG[NHEADING_FLAG - 1] == 1)
                    {
                        NDIR_READ = int.Parse(FileIO.ReadLine(FSR53));// int.Parse(Read(FSR53)[0]);
                        for (IDIR = 0; IDIR < NDIR_READ; IDIR++)
                        {
                            WAVEP_VESSEL_READ[IDIR] = float.Parse(FileIO.Read(FSR53)[0]);
                        }
                        FSBW26.Write(NDIR_READ);
                        FSBW26.Write(JUNK);
                        for (IDIR = 0; IDIR < NDIR_READ; IDIR++)
                        {
                            FSBW26.Write(WAVEP_VESSEL_READ[IDIR]);
                        }
                    }
                    LINE_VALUES = FileIO.Read(FSR53);
                    IBAND_READ = int.Parse(LINE_VALUES[0]);
                    IUSER_WSD_READ = int.Parse(LINE_VALUES[1]);
                    FSBW26.Write(IBAND_READ);
                    FSBW26.Write(IUSER_WSD_READ);

                    LINE_VALUES = FileIO.Read(FSR53);

                    WEIGHTING_READ = float.Parse(LINE_VALUES[0]);
                    COMPASS_ANGLE_READ = float.Parse(LINE_VALUES[1]);
                    ID_WSD_READ = int.Parse(LINE_VALUES[2]);
                    LINE_VALUES = FileIO.Read(FSR53);
                    NSEASTATES_READ = int.Parse(LINE_VALUES[0]);
                    NPEAK_READ = int.Parse(LINE_VALUES[1]);
                    IPERIOD_READ = int.Parse(LINE_VALUES[2]);

                    FSBW26.Write(JUNK);
                    FSBW26.Write(WEIGHTING_READ);
                    FSBW26.Write(COMPASS_ANGLE_READ);
                    FSBW26.Write((ushort)ID_WSD_READ);
                    FSBW26.Write((ushort)NSEASTATES_READ);
                    FSBW26.Write((ushort)NPEAK_READ);
                    FSBW26.Write((ushort)IPERIOD_READ);

                    for (int i = 0; i < NSEASTATES_READ; i++)
                    {
                        LINE_VALUES = FileIO.Read(FSR53);
                        ISPECT1_READ = int.Parse(LINE_VALUES[0]);
                        MSPREAD1_READ = int.Parse(LINE_VALUES[1]);
                        HS_READ = float.Parse(LINE_VALUES[2]);
                        TZ_READ = float.Parse(LINE_VALUES[3]);
                        GAMMA1_READ = float.Parse(LINE_VALUES[4]);
                        NOCCUR_READ = int.Parse(LINE_VALUES[5]);

                        FSBW26.Write((ushort)ISPECT1_READ);
                        FSBW26.Write((ushort)MSPREAD1_READ);
                        FSBW26.Write(JUNK);
                        FSBW26.Write(HS_READ);
                        FSBW26.Write(TZ_READ);
                        FSBW26.Write(GAMMA1_READ);
                        FSBW26.Write(NOCCUR_READ);
                    }

                    if (IHEADING_FLAG[NHEADING_FLAG - 1] == 1)
                    {
                        SUM = 0.0f;
                        for (IDIR = 0; IDIR < NDIR_READ; IDIR++)
                        {
                            SUM = SUM + WAVEP_VESSEL_READ[IDIR];
                        }
                        //
                        FileIO.WriteLine(FSW46, "{0,4}   {1,12:0.000000E+00}   {2,12:0.000000}", ID_WSD_READ, WEIGHTING_READ.ToString("0.000000E+00", CultureInfo.InvariantCulture), COMPASS_ANGLE_READ);

                        for (IDIR = 0; IDIR < NDIR_READ; IDIR++)
                        {
                            if (IDIR > 0 && IDIR % 3 == 0)
                            {
                                FSW46.WriteLine();
                            }
                            FSW46.Write("{0,12:0.000000E+00} ", WAVEP_VESSEL_READ[IDIR] / SUM);
                        }
                        FSW46.WriteLine();
                        FSW46.Flush();
                    }

                }
            }
        }

    }

    public void RouteWSDs(int IFIELD, int iroute, string routeName, int NPT, float inbound, float TIMEIDLE_PERCENTAGE, float returnPeriod, float year, int iflagWaveDir, int iband, int iweightingGrid, string seasonType)
    {
        float[] HEADING_GLOBAL = new float[NDIR + 1];
        float[] WAVEP_VESSEL = new float[NDIR + 1];
        int JP = 0, IP = 0, np = 1;
        TEMP51 = SYSDIR + "TEMP51";
        TEMP52 = SYSDIR + "TEMP52";
        ABSStreamWriter FSW51;
        ABSStreamWriter FSW52;
        ABSStreamReader FSR51;
        ABSStreamReader FSR52;


        EXPOSURE_WEIGHTING = YEAR * (1.0f - TIMEIDLE_PERCENTAGE);
        if (EXPOSURE_WEIGHTING <= 1e-5) EXPOSURE_WEIGHTING = 1e-5f;

        using (FSW51 = FileIO.GetFileStreamWriter(TEMP51, FileMode.Create))
        {
            using (FSW52 = FileIO.GetFileStreamWriter(TEMP52, FileMode.Create))
            {
                FileIO.Read(FSR25);
                FileIO.Read(FSR25);
                // Read route points
                for (JP = 0; JP < NPT; JP++)
                {
                    LINE_VALUES = FileIO.Read(FSR25);
                    YLOC[JP] = float.Parse(LINE_VALUES[0]);
                    I_LATI = int.Parse(LINE_VALUES[1]);
                    XLOC[JP] = float.Parse(LINE_VALUES[2]);
                    I_LONG = int.Parse(LINE_VALUES[3]);
                    if (I_LATI == 2) YLOC[JP] = -YLOC[JP];
                    if (I_LONG == 4) XLOC[JP] = -XLOC[JP];
                }

                // Process route points
                np = 1;
                FileIO.WriteLine(FSW52, $"{XLOC[0]} {YLOC[0]}");
                for (JP = 0; JP < NPT - 1; JP++)
                {
                    A1 = YLOC[JP] * pi / 180.0f;
                    B1 = XLOC[JP] * pi / 180.0f;
                    A2 = YLOC[JP + 1] * pi / 180.0f;
                    B2 = XLOC[JP + 1] * pi / 180.0f;
                    D = 2.0f * (float)Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((A1 - A2) / 2.0), 2) + Math.Cos(A1) * Math.Cos(A2) * Math.Pow(Math.Sin((B1 - B2) / 2.0), 2)));

                    // Determine distance limit based on xloc[jp]

                    if (XLOC[JP] >= -180.0 && XLOC[JP] <= -170.0) DISTANCE_LIMIT = 250.0f;
                    else if (XLOC[JP] > -170.0 && XLOC[JP] <= -160.0) DISTANCE_LIMIT = 500.0f;
                    else if (XLOC[JP] >= 160.0 && XLOC[JP] < 170.0) DISTANCE_LIMIT = 500.0f;
                    else if (XLOC[JP] >= 170.0 && XLOC[JP] <= 180.0) DISTANCE_LIMIT = 250.0f;
                    else DISTANCE_LIMIT = 750.0f;

                    DISTANCE_KM = D * 180.0f * 60.0f / pi * 1.852f;

                    if (DISTANCE_KM <= DISTANCE_LIMIT)
                    {
                        np++;
                        FileIO.WriteLine(FSW52, $"{XLOC[JP + 1]} {YLOC[JP + 1]}");
                    }
                    else
                    {
                        MPT = (int)Math.Round(DISTANCE_LIMIT / DISTANCE_LIMIT) + 1;
                        DL = DISTANCE_KM / (MPT - 1);

                        for (IP = 1; IP < MPT - 1; IP++)
                        {
                            DISTANCE_INTERMEDIATE = DL * IP - 1;
                            F = DISTANCE_INTERMEDIATE / DISTANCE_LIMIT;
                            A = (float)(Math.Sin((1.0f - F) * D) / Math.Sin(D));
                            B = (float)(Math.Sin(F * D) / Math.Sin(D));
                            X = (float)(A * Math.Cos(A1) * Math.Cos(B1) + B * Math.Cos(A2) * Math.Cos(B2));
                            Y = (float)(A * Math.Cos(A1) * Math.Sin(B1) + B * Math.Cos(A2) * Math.Sin(B2));
                            Z = (float)(A * Math.Sin(A1) + B * Math.Sin(A2));
                            YNEW = (float)(Math.Atan2(Z, Math.Sqrt(X * X + Y * Y)) * 180.0f / pi);
                            XNEW = (float)(Math.Atan2(Y, X) * 180.0f / pi);
                            np++;
                            FileIO.WriteLine(FSW52, $"{XNEW} {YNEW}");
                        }
                        np++;
                        FileIO.WriteLine(FSW52, $"{XLOC[JP + 1]} {YLOC[JP + 1]}");
                        FileIO.WriteLine(FSW777, "==NEW POINT===================");
                        FileIO.WriteLine(FSW777, "{0} {1}", XNEW, YNEW);
                        FileIO.WriteLine(FSW777, "==============================");
                    }
                }
            }

            NPT = np;
            using (FSR52 = FileIO.GetFileStreamReader(TEMP52, FileMode.Open))
            {
                for (JP = 0; JP < NPT; JP++)
                {
                    LINE_VALUES = FileIO.Read(FSR52);
                    XLOC[JP] = float.Parse(LINE_VALUES[0]);
                    YLOC[JP] = float.Parse(LINE_VALUES[1]);
                }
                FileIO.WriteLine(FSW777, "== Points in FILE52: distance split ===================");
                for (JP = 0; JP < NPT; JP++)
                {
                    FileIO.WriteLine(FSW777, "{0,4} : {1,6:0.0} {2,6:0.0}", JP, YLOC[JP], XLOC[JP]);
                }
                FSR52.Seek(0);
            }

            // Process grid data
            for (JP = 0; JP < NPT; JP++)
            {
                SITE_LONG = XLOC[JP];
                SITE_LATI = YLOC[JP];
                I_CONER = 0;
                for (int i = 0; i < NGRID; i++)
                {
                    N = ID[i];
                    if (SITE_LATI == LATI[N, 1] || SITE_LATI == LATI[N, 0]) I_CONER++;
                    if (SITE_LONG == LONG[N, 0] || SITE_LONG <= LONG[N, 1]) I_CONER++;
                }
                if (I_CONER != 0)
                {
                    XLOC[JP] = XLOC[JP] - 0.0005f; // new tolerance 0.0005; Apr., 2006
                    YLOC[JP] = YLOC[JP] - 0.0005f;
                }
            }

            // Identify the grid id number for the location of last point in Route Info
            SITE_LONG = XLOC[NPT];
            SITE_LONG = YLOC[NPT];

            for (int i = 0; i < NGRID; i++)
            {
                N = ID[i];
                if (SITE_LATI >= LATI[N, 1] && SITE_LATI <= LATI[N, 0] &&
                    SITE_LONG >= LONG[N, 0] && SITE_LONG <= LONG[N, 1])
                {
                    ID_SITE_LAST = ID[i];
                }
            }

            if (ID_SITE_LAST <= 0)
            {
                Console.WriteLine("Check location of last point in Route: {0}, {1}", SITE_LONG, SITE_LATI);
                //Environment.Exit(0);
            }

            for (JP = 0; JP < NPT - 1; JP++)
            {
                X3 = XLOC[JP]; // xloc & yloc are from route information file
                Y3 = YLOC[JP];
                X4 = XLOC[JP + 1];
                Y4 = YLOC[JP + 1];

                JUMP = 0;
                X = 0; Y = 0;

                if ((X3 > -180.0 && X3 <= -60.0) && (X4 >= 120.0 && X4 < 180.0))
                {
                    X = -180.0f;
                    Y = (Y4 - Y3) / (-(360.0f - X4) - X3) * (X - X3) + Y3; // crossover at x=-180.0
                    JUMP = 1;
                }
                if ((X3 >= 120.0 && X3 < 180.0) && (X4 > -180.0 && X4 <= -60.0))
                {
                    X = 180.0f;
                    Y = (Y4 - Y3) / ((360.0f + X4) - X3) * (X - X3) + Y3; // crossover at x=+180.0
                    JUMP = 2;
                }

                if (JUMP == 1 || JUMP == 2)
                {
                    XEND = X;
                    YEND = Y;
                }
                else
                {
                    XEND = X4;
                    YEND = Y4;
                }

                // Calculate tangential vector of line segment (x3,y3)->(x4,y4)
                PL = (float)(Math.Sqrt(Math.Pow(XEND - X3, 2) + Math.Pow(YEND - Y3, 2)));
                if (JUMP == 0 && PL < 1e-06)
                {
                    Console.WriteLine("Distance between two consecutive locations is so close! {0}, {1}, {2}, {3}, {4}", X3, Y3, XEND, YEND, PL);
                    //Environment.Exit(0);
                }

                XT = (XEND - X3) / PL;
                YT = (YEND - Y3) / PL;

                IWRITE_ONCE = 0;

                X3 += 0.0001f * XT;
                Y3 += 0.0001f * YT;

            // Find the site for (x3,y3).
            G105: SITE_LONG = X3;
                SITE_LATI = Y3;
                ID_SITE = -1;

                for (int i = 0; i < NGRID; i++)
                {
                    N = ID[i];
                    if (SITE_LATI > LATI[N, 1] && SITE_LATI <= LATI[N, 0] && SITE_LONG > LONG[N, 0] && SITE_LONG <= LONG[N, 1])
                    {
                        ID_SITE = ID[i];
                    }
                }

                // id_site <= 0 means that the point does not fall into grid
                if (ID_SITE <= 0)
                {
                    Console.WriteLine("The following intermediate point is outside WSD region. Check input location!");
                    Console.WriteLine("Route number, iroute={0}", iroute);
                    I_LATI_DUM = 1;
                    if (Y3 < 0.0) I_LATI_DUM = 2;
                    I_LONG_DUM = 3;
                    if (X3 < 0.0) I_LONG_DUM = 4;
                    G20: Console.WriteLine("Latitude, 1=North/2=South, Longitude, 3=East/4=West=({0}, {1}, {2}, {3})", Math.Abs(Y3), I_LATI_DUM, Math.Abs(X3), I_LONG_DUM);
                    //Environment.Exit(0);
                }

                if (IWRITE_ONCE == 0 && JP == 0)
                {
                    FileIO.WriteLine(FSW51, "{0} {1} {2}", XLOC[0], YLOC[0], ID_SITE);
                }

                if (JUMP == 1 || JUMP == 2)
                {
                    FileIO.WriteLine(FSW51, "{0} {1} {2}", X, Y, ID_SITE);
                    if (JUMP == 1) X = 180.0f;
                    if (JUMP == 2) X = -180.0f;
                    goto G205;
                }

                SITE_LONG = X4;
                SITE_LATI = Y4;
                ID_SITE2 = -1;

                for (int i = 0; i < NGRID; i++)
                {
                    N = ID[i];
                    if (SITE_LATI > LATI[N, 1] && SITE_LATI <= LATI[N, 0] && SITE_LONG > LONG[N, 0] && SITE_LONG <= LONG[N, 1])
                    {
                        ID_SITE2 = ID[i];
                    }
                }

                if (ID_SITE == ID_SITE2)
                {
                    FileIO.WriteLine(FSW51, "{0} {1} {2}", X4, Y4, ID_SITE);
                    goto G101; // no need for finding intersection
                }

                FileIO.WriteLine(FSW777, "point = {0}", JP);
                FileIO.WriteLine(FSW777, "  id_site  = {0}", ID_SITE);
                FileIO.WriteLine(FSW777, "  id_site2 = {0}", ID_SITE2);

                INTSECT = 0;
                N = ID_SITE;
                X1 = 0; X2 = 0; Y1 = 0; Y2 = 0;

                for (int j = 0; j < 4; j++)
                {
                    if (j == 0)
                    {
                        X1 = LONG[N, 0];
                        Y1 = LATI[N, 0];
                        X2 = LONG[N, 1];
                        Y2 = LATI[N, 0];
                    }
                    else if (j == 1)
                    {
                        X1 = LONG[N, 1];
                        Y1 = LATI[N, 0];
                        X2 = LONG[N, 1];
                        Y2 = LATI[N, 1];
                    }
                    else if (j == 2)
                    {
                        X1 = LONG[N, 1];
                        Y1 = LATI[N, 1];
                        X2 = LONG[N, 0];
                        Y2 = LATI[N, 1];
                    }
                    else if (j == 3)
                    {
                        X1 = LONG[N, 0];
                        Y1 = LATI[N, 1];
                        X2 = LONG[N, 0];
                        Y2 = LATI[N, 0];
                    }

                    DET = (float)((Y4 - Y3) * (X2 - X1) - (X4 - X3) * (Y2 - Y1));

                    if (DET == 0)
                    {
                        UA = 1e6f; UB = 1e6f;
                    }
                    else
                    {
                        UA = ((X4 - X3) * (Y1 - Y3) - (Y4 - Y3) * (X1 - X3)) / DET;
                        UB = ((X2 - X1) * (Y1 - Y3) - (Y2 - Y1) * (X1 - X3)) / DET;
                    }

                    if ((UA >= 0 && UA <= 1) && (UB >= 0 && UB <= 1))
                    {
                        X = X1 + UA * (X2 - X1);
                        Y = Y1 + UA * (Y2 - Y1);
                    }
                }

                FileIO.WriteLine(FSW51, "{0} {1} {2}", X, Y, ID_SITE);


            G205: X3 = X + 0.0001f * XT;
                Y3 = Y + 0.0001f * YT;
                JUMP = 0;
                IWRITE_ONCE = IWRITE_ONCE + 1;

                SITE_LONG = X3;
                SITE_LATI = Y3;
                for (int i = 0; i < NGRID; i++)
                {
                    N = ID[i];
                    if (SITE_LATI > LATI[N, 1] && SITE_LATI <= LATI[N, 0] &&
                        SITE_LONG > LONG[N, 0] && SITE_LONG <= LONG[N, 1])
                    {
                        ID_SITE_NEIGHBOR = ID[i];
                    }
                }

                FileIO.WriteLine(FSW51, "{0} {1} {2}", X, Y, ID_SITE_NEIGHBOR);
                goto G105;

            G101: if (JP == NPT)
                {
                    FileIO.WriteLine(FSW51, "{0} {1} {2}", XLOC[NPT], YLOC[NPT], ID_SITE);
                }
            }
            FSW51.Seek(0);
        }

        using (FSR51 = FileIO.GetFileStreamReader(TEMP51, FileMode.Open))
        {
            LINE_VALUES = FileIO.Read(FSR51);
            XPOS1 = float.Parse(LINE_VALUES[0]);
            YPOS1 = float.Parse(LINE_VALUES[1]);
            ID_SITE1 = int.Parse(LINE_VALUES[2]);
            XINT[0] = XPOS1;
            YINT[0] = YPOS1;
            NPOINT = 0;
            IPOINT = 1;
            NSITE = 0;
        G200: LINE_VALUES = FileIO.Read(FSR51);
            XPOS2 = float.Parse(LINE_VALUES[0]);
            YPOS2 = float.Parse(LINE_VALUES[1]);
            ID_SITE2 = int.Parse(LINE_VALUES[2]);

            NPOINT = NPOINT + 1;
            XINT[NPOINT] = XPOS2;
            YINT[NPOINT] = YPOS2;

            if (ID_SITE2 == ID_SITE1)
            {
                IPOINT = IPOINT + 1;
            }
            else
            {
                NPTS[NSITE] = IPOINT;
                ID_WSD_ROUTE[NSITE] = ID_SITE1;
                NSITE = NSITE + 1;
                IPOINT = 1;
            }

            XPOS1 = XPOS2;
            YPOS1 = YPOS2;
            ID_SITE1 = ID_SITE2;
            if (FSR51.EndOfStream)
                goto G201;
            goto G200;
        G201: NPOINT = NPOINT + 1;
            NPTS[NSITE] = IPOINT;
            ID_WSD_ROUTE[NSITE] = ID_SITE2;
            FSR51.Seek(0);
        }

        int ii = 0;
        using (FSW51 = FileIO.GetFileStreamWriter(TEMP51, FileMode.Create))
        {

            for (ISITE = 0; ISITE <= NSITE; ISITE++)
            {
                FileIO.WriteLine(FSW51, "{0} {1} {2}", ISITE + 1, NPTS[ISITE], ID_WSD_ROUTE[ISITE]);
                for (int j = 0; j < NPTS[ISITE]; j++)
                {
                    ii++;
                    FileIO.WriteLine(FSW51, "{0} {1}", XINT[ii - 1], YINT[ii - 1]);
                }
            }
            FSW51.Seek(0);
        }

        using (FSR51 = FileIO.GetFileStreamReader(TEMP51, FileMode.Open))
        {
            using (FSW52 = FileIO.GetFileStreamWriter(TEMP52, FileMode.Create))
            {
                MSITE = 0;
                NPOINT = 0;

                for (ISITE = 0; ISITE <= NSITE; ISITE++)
                {
                    LINE_VALUES = FileIO.Read(FSR51);
                    IDUM = int.Parse(LINE_VALUES[0]);
                    MPTS = int.Parse(LINE_VALUES[1]);
                    ID_WSD = int.Parse(LINE_VALUES[2]);

                    if (MPTS <= 1)
                    {
                        Console.WriteLine($"Not enough old point in Site No. {ID_WSD}, {ISITE}");
                        //Environment.Exit(0);
                    }

                    for (int j = 0; j < MPTS; j++)
                    {
                        LINE_VALUES = FileIO.Read(FSR51);
                        XINT[j] = float.Parse(LINE_VALUES[0]);
                        YINT[j] = float.Parse(LINE_VALUES[1]);
                    }
                    FileIO.WriteLine(FSW777, "== Points in FILE51: final points ===================");
                    for (int j = 0; j < MPTS; j++)
                    {
                        FileIO.WriteLine(FSW777, "{0,4} : {1,6:0.0} {2,6:0.0}", j, YINT[j], XINT[j]);
                    }

                    int jj = 0;
                    XINT_NEW[0] = XINT[0];
                    YINT_NEW[0] = YINT[0];

                    for (int j = 1; j < MPTS; j++)
                    {
                        PL = (float)Math.Sqrt(Math.Pow((XINT[j] - XINT[j - 1]), 2) + Math.Pow((YINT[j] - YINT[j - 1]), 2));
                        if (PL > 5e-03)
                        {
                            jj++;
                            XINT_NEW[jj] = XINT[j];
                            YINT_NEW[jj] = YINT[j];
                        }
                    }

                    NPTS[ISITE] = jj + 1;


                    if (NPTS[ISITE] >= 1)
                    {
                        MSITE = MSITE + 1;
                        FileIO.WriteLine(FSW52, "{0} {1} {2}", MSITE, NPTS[ISITE], ID_WSD_ROUTE[ISITE]);
                        FileIO.WriteLine(FSW777, "** id_wsd_route = {0}", ID_WSD_ROUTE[ISITE]);
                        for (int j = 0; j < NPTS[ISITE]; j++)
                        {
                            FileIO.WriteLine(FSW52, "{0} {1}", XINT_NEW[j], YINT_NEW[j]);
                            FileIO.WriteLine(FSW777, "{0} {1}", XINT_NEW[j], YINT_NEW[j]);

                            NPOINT = NPOINT + 1;
                        }
                        FileIO.WriteLine(FSW777, "****************************************************");
                    }
                }
                FSW52.Seek(0);
            }
            FSR51.Seek(0);
        }

        using (FSR52 = FileIO.GetFileStreamReader(TEMP52, FileMode.Open))
        {
            NWSD = 0;

            for (ISITE = 0; ISITE < MSITE; ISITE++)
            {
                LINE_VALUES = FileIO.Read(FSR52);
                IDUM = int.Parse(LINE_VALUES[0]);
                MPTS = int.Parse(LINE_VALUES[1]);
                ID_WSD = int.Parse(LINE_VALUES[2]);

                for (int j = 0; j < MPTS; j++)
                {
                    NWSD = NWSD + 1;
                    LINE_VALUES = FileIO.Read(FSR52);
                    XINT[j] = float.Parse(LINE_VALUES[0]);
                    YINT[j] = float.Parse(LINE_VALUES[1]);
                }
                NWSD = NWSD - 1;

            }

            if (INBOUND == 1) NWSD = 2 * NWSD;
            FSR52.Seek(0);
        }
        int IFLAG;
        for (IFLAG = 0; IFLAG < NHEADING_FLAG; IFLAG++)
        {
            IHEADING_FLAG[IFLAG] = 0;
        }
        IHEADING_FLAG[NHEADING_FLAG - 2] = 1;
        if (IFLAG_WAVEDIR != 0) IHEADING_FLAG[NHEADING_FLAG - 1] = 1;

        FileIO.WriteLine(FSW53, "{0}", NWSD);
        FileIO.WriteLine(FSW53, "{0} {1} {2} {3}", IROUTE, RETURN_PERIOD, YEAR, TIMEIDLE_PERCENTAGE);
        for (int i = 0; i < NHEADING_FLAG; i++)
        {
            FileIO.WriteLine(FSW53, "{0}", IHEADING_FLAG[i]);
        }

        if (IFIELD == 2) NWSD_TOTAL_TRANSIT = NWSD_TOTAL_TRANSIT + NWSD;
        using (FSR52 = FileIO.GetFileStreamReader(TEMP52, FileMode.Open))
        {
            for (ISITE = 0; ISITE < MSITE; ISITE++)
            {

                LINE_VALUES = FileIO.Read(FSR52);
                IDUM = int.Parse(LINE_VALUES[0]);
                MPTS = int.Parse(LINE_VALUES[1]);
                ID_WSD = int.Parse(LINE_VALUES[2]);

                for (int j = 0; j < MPTS; j++)
                {
                    LINE_VALUES = FileIO.Read(FSR52);
                    XINT[j] = float.Parse(LINE_VALUES[0]);
                    YINT[j] = float.Parse(LINE_VALUES[1]);
                }

                for (int j = 0; j < MPTS - 1; j++)
                {
                    PL = (float)(Math.Sqrt(Math.Pow(XINT[j + 1] - XINT[j], 2) + Math.Pow(YINT[j + 1] - YINT[j], 2)));
                    if (PL < 1e-6) PL = 1e-6f;

                    XT = (XINT[j + 1] - XINT[j]) / PL;
                    YT = (YINT[j + 1] - YINT[j]) / PL;

                    A1 = (float)(YINT[j] * Math.PI / 180.0f);
                    B1 = (float)(XINT[j] * Math.PI / 180.0f);
                    A2 = (float)(YINT[j + 1] * Math.PI / 180.0);
                    B2 = (float)(XINT[j + 1] * Math.PI / 180.0);


                    D = (float)(2.0f * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((A1 - A2) / 2.0), 2) +
                                    Math.Cos(A1) * Math.Cos(A2) * Math.Pow(Math.Sin((B1 - B2) / 2.0), 2))));

                    DISTANCE_KM = (float)(D * 180.0f * 60.0f / Math.PI * 1.852f);
                    COURSE_ANGLE = (float)(Math.Atan2(YT, XT) * 180.0f / Math.PI);

                    if (YT < 0.0)
                        COURSE_ANGLE += 360.0f; // to define course angle between 0 to 360

                    COMPASS_ANGLE = 90.0f - COURSE_ANGLE;

                    if (COMPASS_ANGLE < 0.0)
                        COMPASS_ANGLE += 360.0f;
                    string sctFileName = GetWaveFileName(WAVETYPE, seasonType);
                    if (WAVETYPE == "ABS")
                        FILE15 = SYSDIR.Trim() + "abswave.sct";
                    else if (WAVETYPE == "BMT")
                        FILE15 = SYSDIR.Trim() + sctFileName;
                    _logger.LogInformation($"Using wave file: {FILE15}");
                    if (!File.Exists(FILE15))
                    {
                        _logger.LogError($"Wave file not found: {FILE15}");
                        throw new FileNotFoundException($"Wave file not found: {FILE15}");
                    }
                    using (FSR15 = FileIO.GetFileStreamReader(FILE15, FileMode.Open))
                    {
                        for (int i = 0; i < NGRID; i++)
                        {
                            ID[i] = int.Parse(FileIO.ReadLine(FSR15));

                            for (IDIR = 0; IDIR < NDIR; IDIR++)
                            {
                                LINE_VALUES = FileIO.Read(FSR15);
                                WAVEP[IDIR] = float.Parse(LINE_VALUES[0]);
                                WINDP[IDIR] = float.Parse(LINE_VALUES[1]);

                                WAVEP1[IDIR] = WAVEP[IDIR]; // DOUBLE -> SINGLE PRECISION
                                WINDP1[IDIR] = WINDP[IDIR];

                                HEADING_GLOBAL[IDIR] = 360.0F / NDIR * (IDIR - 1 + 1); // Calculate heading //
                            }

                            NDIR1 = NDIR + 1;
                            HEADING_GLOBAL[NDIR1 - 1] = 360.0F;
                            WAVEP1[NDIR1 - 1] = WAVEP1[0];
                            WINDP1[NDIR1 - 1] = WINDP1[0];

                            if (ID_WSD == ID[i])
                            {

                                for (IDIR = 0; IDIR < NDIR; IDIR++)
                                {
                                    THETA = (COURSE_ANGLE + 180.0f) + 360.0f / NDIR * (IDIR); // start from following seas
                                    if (THETA >= 360.0 && THETA < 720.0)
                                        THETA -= 360.0f;
                                    if (THETA >= 720.0 && THETA < 1080.0)
                                        THETA -= 720.0f;

                                    BETA = 90.0f - THETA;
                                    if (BETA < 0.0)
                                        BETA += 360.0f;

                                    LSPLINE(HEADING_GLOBAL, WAVEP1, NDIR1, BETA, ref Y0);

                                    WAVEP_VESSEL[IDIR] = Y0;
                                }
                                if (IHEADING_FLAG[NHEADING_FLAG - 1] == 1)
                                {
                                    FileIO.WriteLine(FSW53, "{0}", NDIR);
                                    FileIO.WriteLine(FSW53, "{0}", WAVEP_VESSEL[0]);
                                    for (IDIR = NDIR; IDIR > 1; IDIR--)
                                    {
                                        FileIO.WriteLine(FSW53, "{0}", WAVEP_VESSEL[IDIR - 1]);
                                    }

                                }
                                IUSER_WSD = 0;

                                FileIO.WriteLine(FSW53, "{0} {1}", IBAND, IUSER_WSD);
                            }
                        }
                        FSR15.Seek(0);
                    }
                    if (WAVETYPE == "ABS")
                        FILE15 = SYSDIR.Trim() + "abswave.sct";
                    else if (WAVETYPE == "BMT")
                        FILE15 = SYSDIR.Trim() + sctFileName;

                    using (FSBR15 = FileIO.GetBinaryFileStreamReader(FILE15, FileMode.Open))
                    {
                        FSBR15.ReadJunkData();
                        using (FSW51 = FileIO.GetFileStreamWriter(TEMP51, FileMode.Open))
                        {
                            for (int i = 0; i < NGRID; i++)
                            {
                                for (IFLAG = 0; IFLAG < NHEADING_FLAG; IFLAG++)
                                {
                                    IHEADING_FLAG[IFLAG] = 0;
                                }
                                if (IFLAG_WAVEDIR == 0)
                                    IHEADING_FLAG[NHEADING_FLAG - 2] = 1;
                                else
                                    IHEADING_FLAG[NHEADING_FLAG - 1] = 1;

                                ID[i] = FSBR15.ReadIntegerValue();
                                FSBR15.SwitchToNextLine();

                                for (IHGT = 0; IHGT < NHGT; IHGT++)
                                {
                                    for (IPER = 0; IPER < NPER; IPER++)
                                    {
                                        NOCCUR[IHGT, IPER] = FSBR15.ReadIntegerValue();
                                    }
                                    HMARGIN[IHGT] = FSBR15.ReadIntegerValue();

                                    FSBR15.SwitchToNextLine();
                                }

                                for (IPER = 0; IPER < NPER; IPER++)
                                {
                                    TMARGIN[IPER] = FSBR15.ReadIntegerValue();
                                }
                                TOTAL[i] = FSBR15.ReadIntegerValue();

                                FSBR15.SwitchToNextLine();

                                if (ID_WSD == ID[i])
                                {
                                    FSW19.WriteLine("{0} {1} {2}", ID[i], EXPOSURE_WEIGHTING * DISTANCE_KM, COMPASS_ANGLE);
                                    NSEASTATES = 0;
                                    for (IPER = 0; IPER < NPER; IPER++)
                                    {
                                        if (IPER > 0 && IPER % 20 == 0)
                                        {
                                            //FSW19.WriteLine();
                                        }
                                        FSW19.Write("{0,6:0.0}  ", TZ[IPER]);
                                    }
                                    FSW19.WriteLine();
                                    FSW19.Flush();
                                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                                    {
                                        FSW19.Write("{0}              ", HS[IHGT]);
                                        for (IPER = 0; IPER < NPER; IPER++)
                                        {
                                            if (IPER > 0 && IPER % 20 == 0)
                                            {
                                                //FSW19.WriteLine();
                                            }
                                            FSW19.Write("{0,6:0}  ", NOCCUR[IHGT, IPER]);
                                        }
                                        FSW19.WriteLine();
                                        FSW19.Flush();

                                        for (IPER = 0; IPER < NPER; IPER++)
                                        {
                                            if (NOCCUR[IHGT, IPER] > 0)
                                            {
                                                NSEASTATES++;
                                                FileIO.WriteLine(FSW51, "{0} {1} {2}", HS[IHGT], TZ[IPER], NOCCUR[IHGT, IPER]);
                                            }
                                        }
                                    }
                                }
                            }
                            FSW51.Seek(0);
                        }
                        FileIO.WriteLine(FSW53, "{0} {1} {2}", EXPOSURE_WEIGHTING * DISTANCE_KM, COMPASS_ANGLE, ID_WSD);
                        FileIO.WriteLine(FSW53, "{0} {1} {2}", NSEASTATES, NPEAK, 2);
                        using (FSR51 = FileIO.GetFileStreamReader(TEMP51, FileMode.Open))
                        {
                            for (int i = 0; i < NSEASTATES; i++)
                            {
                                LINE_VALUES = FileIO.Read(FSR51);
                                HS_WRITE[i] = float.Parse(LINE_VALUES[0]);
                                TZ_WRITE[i] = float.Parse(LINE_VALUES[1]);
                                NOCCUR_WRITE[i] = int.Parse(LINE_VALUES[2]);
                                FileIO.WriteLine(FSW53, "{0} {1} {2} {3} {4} {5}", ISPECT1, MSPREAD1, HS_WRITE[i], TZ_WRITE[i], GAMMA1, NOCCUR_WRITE[i]);
                            }
                        }

                        if (INBOUND == 1)
                        {
                            if (IHEADING_FLAG[NHEADING_FLAG - 1] == 1)
                            {
                                FileIO.WriteLine(FSW53, "{0}", NDIR);
                                for (IDIR = NDIR; IDIR > 0; IDIR--)
                                {
                                    FileIO.WriteLine(FSW53, "{0}", WAVEP_VESSEL[IDIR - 1]);
                                }
                            }

                            IUSER_WSD = 0;
                            FileIO.WriteLine(FSW53, "{0} {1}", IBAND, IUSER_WSD);
                            COMPASS_ANGLE = COMPASS_ANGLE + 180.0f;
                            if (COMPASS_ANGLE >= 360.0 && COMPASS_ANGLE < 720.0)
                            {
                                COMPASS_ANGLE -= 360.0f;
                            }
                            FileIO.WriteLine(FSW53, "{0} {1} {2}", EXPOSURE_WEIGHTING * DISTANCE_KM, COMPASS_ANGLE, ID_WSD);
                            FileIO.WriteLine(FSW53, "{0} {1} {2}", NSEASTATES, NPEAK, 2);

                            for (int i = 0; i < NSEASTATES; i++)
                            {
                                FileIO.WriteLine(FSW53, "{0} {1} {2} {3} {4} {5}", ISPECT1, MSPREAD1, HS_WRITE[i], TZ_WRITE[i], GAMMA1, NOCCUR_WRITE[i]);
                            }

                            for (int i = 0; i < NGRID; i++)
                            {
                                if (ID_WSD == ID[i])
                                {
                                    FileIO.WriteLine(FSW19, "{0} {1} {2}", ID[i], EXPOSURE_WEIGHTING * DISTANCE_KM, COMPASS_ANGLE);
                                    for (IPER = 0; IPER < NPER; IPER++)
                                    {
                                        if (IPER > 0 && IPER % 20 == 0)
                                        {
                                            FSW19.WriteLine();
                                        }
                                        FSW19.Write("              {0,6:0.0} ", TZ[IPER]);
                                    }
                                    FSW19.WriteLine();
                                    FSW19.Flush();
                                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                                    {
                                        for (IPER = 0; IPER < NPER; IPER++)
                                        {
                                            FileIO.WriteLine(FSW19, "{0} ", HS[IHGT]);
                                            if (IPER > 0 && IPER % 20 == 0)
                                            {
                                                FSW19.WriteLine();
                                            }
                                            FSW19.Write("              {0,6:0.0} ", NOCCUR[IHGT, IPER]);
                                        }
                                        FSW19.WriteLine();
                                        FSW19.Flush();
                                    }
                                }
                            }
                        }
                        FSBR15.Close();
                    }
                }
            }
        }
    }
    public void LSPLINE(float[] X, float[] Y, int N, float X0, ref float Y0)
    {
        for (int i = 0; i < N - 1; i++)
        {
            if (X0 >= X[i] && X0 <= X[i + 1])
            {
                Y0 = (Y[i + 1] - Y[i]) / (X[i + 1] - X[i]) * (X0 - X[i]) + Y[i];
            }
        }
    }
    public void PRintWSDS(string WOKDIR)
    {
        int NPNT = 200;
        float NTOTAL = 100000;
        int NROUTE = 50;
        int NGRIDS = 1200;

        float[,] WFNI = new float[NROUTE, NPNT];
        float[,] WFN = new float[NROUTE, NPNT];
        float[] WFIG = new float[NGRIDS];
        int[,] ISFLAG = new int[NROUTE, NPNT];
        int IG = 0;

        float WFSUM = 0.0f;
        float SUMNIJ = 0.0f;
        int[] NPT = new int[NPNT];
        int[,] ID_ = new int[NROUTE, NPNT];
        float[,] WF = new float[NROUTE, NPNT];
        float[,] COMPASS_ANGLE = new float[NROUTE, NPNT];
        float[,] COMPOSITEWSD = new float[NHGT, NPER];
        float[,] OCCUR = new float[NHGT, NPER];
        float[,] OCCUR0 = new float[NHGT, NPER];
        float[] WFI = new float[NROUTE];
        float[] PROBSUM = new float[NPNT];
        string[] NAME = new string[NPNT];
        float[] SUMN = new float[NPNT];
        float[] HSUM = new float[NHGT];
        float[] TSUM = new float[NPER];

        NROUTES = NRTS_TRA + NRTS_RTE;
        ROUTE_TYPE = "TRANSIT ROUTE";
        string TMP12;

        if (NROUTES <= 0)
        {
            Console.WriteLine("NO ROUTE DATA AVAILABLE. PLEASE CHECK ROUTE DATA.");
            //Environment.Exit(0);
        }
        string FILE35 = Path.Combine(WOKDIR, "intermediate.wsd");

        string FILE29 = Path.Combine(WOKDIR, "composite.wsd");
        string FILEOUT = Path.Combine(WOKDIR, "composite.sct");
        using (FSW77 = FileIO.GetFileStreamWriter(FILEOUT, FileMode.Create))
        {
            string FILEOUTSUMMARY = Path.Combine(WOKDIR, "summary.txt");
            string FILEOUTDETAIL = Path.Combine(WOKDIR, "detail.txt");

            for (IHGT = 0; IHGT < NHGT; IHGT++)
            {
                for (int IPER = 0; IPER < NPER; IPER++)
                {
                    COMPOSITEWSDA[IHGT, IPER] = 0;
                }
            }
            PROBSUMA = 0.0f;
            using (FSR39 = FileIO.GetFileStreamReader("TEMP39", FileMode.Open))
            {
                for (int i = 0; i < NROUTES; i++)
                {
                    LINE_VALUES = FileIO.Read(FSR39);
                    NPT[i] = int.Parse(LINE_VALUES[0]);
                    TMP12 = LINE_VALUES[1];
                    Console.WriteLine($"{NPT[i]} {TMP12[i]}");
                }
            }
            FileIO.WriteLine(FSW77, "COMPOSITE WAVE DATA NORMALIZED TO 100,000");
            FileIO.WriteLine(FSW77, "== 1. INDIVIDUAL ROUTE ===============================================================================================================================");
            WFSUM = 0;
            Array.Clear(WFI, 0, WFI.Length); // Sets all elements to 0
            for (int i = 0; i < NROUTES; i++)
            {
                for (IHGT = 0; IHGT < NHGT; IHGT++)
                {
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        COMPOSITEWSD[IHGT, IPER] = 0.0f;
                    }
                }
                using (FSR35 = FileIO.GetFileStreamReader(FILE35, FileMode.Open))
                {
                    NAME[i] = FSR35.ReadLine().Trim();
                    for (int j = 0; j < NPT[i]; j++)
                    {
                        LINE_VALUES = FileIO.Read(FSR35);
                        ID_[i, j] = int.Parse(LINE_VALUES[0]);
                        WF[i, j] = float.Parse(LINE_VALUES[1]);
                        COMPASS_ANGLE[i, j] = float.Parse(LINE_VALUES[2]);

                        var tmp = FileIO.Read(FSR35);
                        for (IPER = 0; IPER < NPER; IPER++)
                        {
                            TZ[IPER] = float.Parse(tmp[IPER]);
                        }

                        WFI[i] = WFI[i] + WF[i, j];
                        WFSUM = WFSUM + WF[i, j];

                        for (IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            LINE_VALUES = FileIO.Read(FSR35);
                            HS[IHGT] = float.Parse(LINE_VALUES[0]);
                            for (IPER = 0; IPER < NPER; IPER++)
                            {
                                OCCUR0[IHGT, IPER] = float.Parse(LINE_VALUES[IPER + 1]);
                            }
                        }

                        SUMNIJ = 0.0f;
                        for (IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            for (IPER = 0; IPER < NPER; IPER++)
                            {
                                SUMNIJ = SUMNIJ + OCCUR0[IHGT, IPER];
                            }
                        }

                        for (int IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            for (int IPER = 0; IPER < NPER; IPER++)
                            {
                                if (Math.Abs(SUMNIJ) > 1.0E-6)
                                {
                                    OCCUR[IHGT, IPER] = OCCUR0[IHGT, IPER] * NTOTAL / SUMNIJ;
                                }
                                else
                                {
                                    Console.WriteLine($"ERROR: SUMNIJ IS ZERO: IROUTE = {i}, JGRID = {j}");
                                }
                            }
                        }

                        SUMNIJ = 0.0f;
                        for (IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            for (IPER = 0; IPER < NPER; IPER++)
                            {
                                SUMNIJ = SUMNIJ + OCCUR[IHGT, IPER];
                            }
                        }

                        for (IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            for (IPER = 0; IPER < NPER; IPER++)
                            {
                                COMPOSITEWSD[IHGT, IPER] = COMPOSITEWSD[IHGT, IPER] + WF[i, j] * OCCUR[IHGT, IPER];
                                COMPOSITEWSDA[IHGT, IPER] = COMPOSITEWSDA[IHGT, IPER] + WF[i, j] * OCCUR[IHGT, IPER];
                            }
                        }
                        string FilePath = WOKDIR + "detail.txt";
                        using (FSW83 = FileIO.GetFileStreamWriter(FilePath, FileMode.Create))
                        {
                            FileIO.WriteLine(FSW83, FF190, NAME[i], ID_[i, j]);

                            for (IPER = 0; IPER < NPER; IPER++)
                            {
                                FileIO.WriteLine(FSW83, "{0}", TZ[IPER]);
                            }

                            for (IHGT = 0; IHGT < NHGT; IHGT++)
                            {
                                FileIO.WriteLine(FSW83, "{0}         ", HS[IHGT]);
                                for (IPER = 0; IPER < NPER; IPER++)
                                {
                                    FileIO.WriteLine(FSW83, "{0}", WF[i, j] * OCCUR[IHGT, IPER]);
                                }
                                FSW83.WriteLine();
                            }
                        }
                    }
                }
                Array.Clear(PROBSUM, 0, PROBSUM.Length); // Sets all elements to 0

                for (IHGT = 0; IHGT < NHGT; IHGT++)
                {
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        PROBSUM[i] = PROBSUM[i] + COMPOSITEWSD[IHGT, IPER];
                    }
                }
                FILE29 = WOKDIR + "composite.wsd";
                using (FSW29 = FileIO.GetFileStreamWriter(FILE29, FileMode.Create))
                {
                    FileIO.WriteLine(FSW29, FF190, NAME[i], ROUTE_TYPE);
                    FileIO.WriteLine(FSW29, "== 1. Non-normalized occurrence");
                    FileIO.Write(FSW29, @" Hs(m)\Tz(sec)   ");
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        FileIO.Write(FSW29, "{0,6:0.0}", TZ[IPER]);
                        if (IPER != NPER - 1)
                        {
                            FileIO.Write(FSW29, "        ");
                        }
                    }

                    FileIO.WriteLine(FSW29);
                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                    {
                        FileIO.Write(FSW29, "{0,6:0.0}         ", HS[IHGT]);
                        for (IPER = 0; IPER < NPER; IPER++)
                        {
                            var strValue = "0" + string.Format("{0:.00000E+00}", COMPOSITEWSD[IHGT, IPER]);
                            FileIO.Write(FSW29, "{0,12:E5}", strValue);
                            if (IPER != NPER - 1)
                            {
                                FileIO.Write(FSW29, "  ");
                            }
                        }
                        FileIO.WriteLine(FSW29);
                    }
                    FileIO.WriteLine(FSW29, "------------------------------------------------------------------------------------------------------------------------------------------------------");


                    //===== Normalize compositeWSD to 1.0
                    SUM = 0.0f;
                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                    {
                        for (IPER = 0; IPER < NPER; IPER++)
                        {
                            if (Math.Abs(PROBSUM[i]) > 1.0E-9)
                                COMPOSITEWSD[IHGT, IPER] = COMPOSITEWSD[IHGT, IPER] / PROBSUM[i];

                            else
                            {
                                Console.WriteLine($"PROBSUM IS ZERO AT ROUTE: {NAME[i]}");
                                //Environment.Exit(0);
                            }
                            SUM += COMPOSITEWSD[IHGT, IPER];
                        }
                    }

                    //===== row- and column-sum calculation
                    SUMN[i] = 0.0f;
                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                    {
                        HSUM[IHGT] = 0.0f;
                        for (IPER = 0; IPER < NPER; IPER++)
                        {
                            HSUM[IHGT] += COMPOSITEWSD[IHGT, IPER] * NTOTAL;
                        }
                        SUMN[i] += HSUM[IHGT];
                    }

                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        TSUM[IPER] = 0.0f;
                        for (IHGT = 0; IHGT < NHGT; IHGT++)
                        {
                            TSUM[IPER] += COMPOSITEWSD[IHGT, IPER] * NTOTAL;
                        }
                    }

                    if (i > NRTS_TRA) ROUTE_TYPE = "Historical route";

                    FileIO.WriteLine(FSW29, "== 2. Normalized occurrence ");

                    FileIO.WriteLine(FSW77, "# {0}", NAME[i]);
                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                    {
                        FileIO.WriteLine(FSW77, "{0}", HS[IHGT]);
                    }
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        FileIO.WriteLine(FSW77, "{0}", TZ[IPER]);
                    }

                    FileIO.Write(FSW29, @" Hs(m)\Tz(sec)   ");
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        FileIO.Write(FSW29, "{0,6:0.0}", TZ[IPER]);
                        if (IPER != NPER - 1)
                        {
                            FileIO.Write(FSW29, "        ");
                        }
                    }

                    FileIO.WriteLine(FSW29);

                    for (IHGT = 0; IHGT < NHGT; IHGT++)
                    {
                        FileIO.Write(FSW29, "{0,6:0.0}         ", HS[IHGT]);
                        for (IPER = 0; IPER < NPER; IPER++)
                        {
                            var strValue = "0" + string.Format("{0:.00000E+00}", COMPOSITEWSD[IHGT, IPER]);
                            FileIO.Write(FSW29, "{0,12:E5}", strValue);
                            if (IPER != NPER - 1)
                            {
                                FileIO.Write(FSW29, "  ");
                            }
                        }
                        FileIO.WriteLine(FSW29);

                        for (int IPER = 0; IPER < NPER; IPER++)
                        {
                            FileIO.Write(FSW77, "{0}", COMPOSITEWSD[IHGT, IPER] * 100000);
                        }
                        FileIO.Write(FSW77, "{0}", HSUM[IHGT]);
                    }

                    FileIO.WriteLine(FSW29, "======================================================================================================================================================");
                    for (IPER = 0; IPER < NPER; IPER++)
                    {
                        FileIO.WriteLine(FSW77, "{0}", TSUM[IPER]);
                    }
                    FileIO.WriteLine(FSW77, "{0}", SUMN[i]);
                    FileIO.WriteLine(FSW77, "------------------------------------------------------------------------------------------------------------------------------------------------------");
                    PROBSUMA = PROBSUMA + PROBSUM[i];
                }
            }

            string FileName = WOKDIR.Trim() + "summary.txt";
            using (FSW81 = FileIO.GetFileStreamWriter(FileName, FileMode.Create))
            {
                FileIO.WriteLine(FSW81, "=========================================================================================");
                FileIO.WriteLine(FSW81, "-- SUMMARY OF WEIGHTING for ROUTES AND GRIDS --------------------------------------------");
                FileIO.WriteLine(FSW81, "=========================================================================================");

                for (int i = 0; i < NROUTES; i++)
                {
                    FileIO.WriteLine(FSW81, "-----------------------------------------------------------------------------------------");
                    FileIO.WriteLine(FSW81, NAME[i].Trim());
                    FileIO.WriteLine(FSW81, "-----------------------------------------------------------------------------------------");
                    FileIO.WriteLine(FSW81, "{0,16}{1,16}{2,16}{3,16}{4,16}", "GRID_ID", "WEIGHTING_RAW", "WEIGHTING_ROUTE", "WEIGHTING_TOTAL", "COMPASS_ANGLE");
                    FileIO.WriteLine(FSW81, "-----------------------------------------------------------------------------------------");

                    for (int j = 0; j < NPT[i]; j++)
                    {
                        if (Math.Abs(WFSUM) > 1.0E-6 && Math.Abs(WFI[i]) > 1.0E-6)
                        {
                            WFNI[i, j] = WF[i, j] / WFI[i];
                            WFN[i, j] = WF[i, j] / WFSUM;
                            FileIO.WriteLine(FSW81, "{0,16}{1,16:F4}{2,16:F4}{3,16:F4}{4,16:F4}", ID_[i, j], WF[i, j], WFNI[i, j], WFN[i, j], COMPASS_ANGLE[i, j]);
                        }
                        else
                        {
                            Console.WriteLine("************ ERROR: WFSUM = 0 OR WFI = 0 AT ROUTE = " + i);
                            //Environment.Exit(0);
                        }
                    }
                }

                Array.Clear(WFIG, 0, WFIG.Length);
            }
        }
    }

    private string GetWaveFileName(string waveType, string seasonType)
    {
        if (waveType == "ABS")
            return "abswave.sct";
        if (waveType == "BMT")
        {
            switch (seasonType.ToLower())
            {
                case "annual": return "BMTwave0.sct";
                case "spring": return "BMTwave1.sct";
                case "summer": return "BMTwave2.sct";
                case "fall": return "BMTwave3.sct";
                case "winter": return "BMTwave4.sct";
                default: return "BMTwave0.sct";
            }
        }
        return "";
    }
}