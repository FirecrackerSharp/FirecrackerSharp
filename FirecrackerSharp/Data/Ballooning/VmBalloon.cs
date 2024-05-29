namespace FirecrackerSharp.Data.Ballooning;

public record VmBalloon(
    int AmountMib,
    bool DeflateOnOom,
    int StatsPollingIntervalS = 0);
