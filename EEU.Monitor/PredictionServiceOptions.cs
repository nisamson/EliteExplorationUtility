namespace EEU.Monitor;

public class PredictionServiceOptions {
    public const string Position = "PredictionService";

    public string DataStorePath { get; set; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        nameof(EEU),
        nameof(Monitor),
        "dataStore"
    );

    public bool PrimeCache { get; set; } = true;
    public DateTime PrimeEpoch { get; set; } = DateTime.UtcNow.AddDays(-7);
    
    public bool ShutdownOnShutdown { get; set; } = false;
}
