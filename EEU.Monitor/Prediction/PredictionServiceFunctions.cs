using FASTER.core;

namespace EEU.Monitor.Prediction;

public class PredictionServiceFunctions : SimpleFunctions<string, Elite.System> {
    private static Elite.System Merge(Elite.System a, Elite.System b) {
        return a.Merge(b);
    }

    private PredictionServiceFunctions() : base(Merge) { }

    public static PredictionServiceFunctions Instance { get; } = new();
}
