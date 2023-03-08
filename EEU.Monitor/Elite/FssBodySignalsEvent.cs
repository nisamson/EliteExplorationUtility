// EliteExplorationUtility - EEU.Monitor - FssBodySignalsEvent.cs
// Copyright (C) 2023 Nick Samson
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using EliteAPI.Abstractions.Events;
using Newtonsoft.Json;

namespace EEU.Monitor.Elite;

// { "timestamp":"2023-02-20T05:11:07Z", "event":"FSSBodySignals", "BodyName":"Eol Prou SX-W b17-57 B 3", "BodyID":28, "SystemAddress":125887770477721, "Signals":[ { "Type":"$SAA_SignalType_Biological;", "Type_Localised":"Biological", "Count":1 } ] }

public class FssBodySignalsEvent : IEvent {
    public string BodyName { get; set; } = "";

    [JsonProperty("BodyID")] public int BodyId { get; set; }
    public string SystemAddress { get; set; } = "";
    public Signals[] Signals { get; set; } = Array.Empty<Signals>();
    [JsonProperty("timestamp")] public DateTime Timestamp { get; set; }
    [JsonProperty("event")] public string Event { get; set; } = "FSSBodySignals";
}

public class Signals {
    public string Type { get; set; } = "";
    [JsonProperty("Type_Localised")] public string TypeLocalized { get; set; } = "";
    public long Count { get; set; }

    public bool IsBiological => TypeLocalized == "Biological";
}
