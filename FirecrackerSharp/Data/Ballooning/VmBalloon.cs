namespace FirecrackerSharp.Data.Ballooning;

public sealed record VmBalloon(
    int AmountMib,
    bool DeflateOnOom,
    int StatsPollingIntervalS = 0);
