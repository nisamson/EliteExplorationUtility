using EliteAPI.Abstractions.Events;
using Newtonsoft.Json;

namespace EEU.Monitor.Elite;

// { "timestamp":"2023-02-20T05:11:07Z", "event":"FSSBodySignals", "BodyName":"Eol Prou SX-W b17-57 B 3", "BodyID":28, "SystemAddress":125887770477721, "Signals":[ { "Type":"$SAA_SignalType_Biological;", "Type_Localised":"Biological", "Count":1 } ] }

public class FssBodySignalsEvent : IEvent {
    [JsonProperty("timestamp")] public DateTime Timestamp { get; set; }
    [JsonProperty("event")] public string Event { get; set; } = "FSSBodySignals";

    public string BodyName { get; set; } = "";

    [JsonProperty("BodyID")] public int BodyId { get; set; }
    public string SystemAddress { get; set; } = "";
    public Signals[] Signals { get; set; } = Array.Empty<Signals>();
}

public class Signals {
    public string Type { get; set; } = "";
    [JsonProperty("Type_Localised")] public string TypeLocalized { get; set; } = "";
    public long Count { get; set; }

    public bool IsBiological => TypeLocalized == "Biological";
}
