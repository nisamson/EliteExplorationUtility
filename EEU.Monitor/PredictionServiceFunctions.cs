using FASTER.core;

namespace EEU.Monitor;

public class PredictionServiceFunctions : SimpleFunctions<string, System> {
    private static System Merge(System a, System b) {
        return a.Merge(b);
    }

    private PredictionServiceFunctions() : base(Merge) { }

    public static PredictionServiceFunctions Instance { get; } = new();
}
