namespace EEU.Monitor.Prediction;

public class PredictionServiceOptions {
    public const string Position = "PredictionService";
    public bool PrimeCache { get; set; } = true;
    public DateTime PrimeEpoch { get; set; } = DateTime.UtcNow.AddDays(-7);
}
