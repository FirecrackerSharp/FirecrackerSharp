namespace FirecrackerSharp.Data;

public record VmBalloon(
    int AmountMib,
    bool DeflateOnOom,
    int StatsPollingIntervalS = 0);

public record VmBalloonUpdate(
    int AmountMib);
