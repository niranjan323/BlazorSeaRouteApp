namespace SeaRouteWebApis.Interfaces;

public interface IBkWxRoutes
{
    // Modified by Niranjan - Added seasonType parameter to support seasonal wave data
    void ProcessWaveData(string sessionFolderPath, string waveData, string seasonType = "annual");
}