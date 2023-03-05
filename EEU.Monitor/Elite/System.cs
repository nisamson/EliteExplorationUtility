using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using EEU.Learn.Model;
using EEU.Monitor.Util;
using LiteDB;
using NeoSmart.AsyncLock;
using Serilog;

namespace EEU.Monitor.Elite;

public class System {
    public System(string? name, string systemAddress) {
        this.name = name;
        SystemAddress = systemAddress;
    }

    public static System WithAddress(string systemAddress) {
        return new System(null, systemAddress);
    }


    public System Merge(System other) {
        Log.Logger.Verbose("Merging {System} with {Other}", this, other);
        var result = new System(name.Or(other.name), SystemAddress);
        lock (bodiesLock) {
            Log.Logger.Verbose("Adding my bodies to result");
            foreach (var (name, body) in Bodies) {
                Log.Logger.Verbose("Adding body {body} to result", body.Name);
                result.UpdateBody(name, body);
            }
        }

        Log.Logger.Verbose("Taking lock on other bodies");
        lock (other.bodiesLock) {
            Log.Logger.Verbose("Adding other bodies to result");
            foreach (var (name, body) in other.Bodies) {
                Log.Logger.Verbose("Adding body {body} to result", body.Name);
                result.UpdateBody(name, body);
            }
        }

        Log.Logger.Verbose("finished merge of {System} with {Other}", this, other);

        return result;
    }

    [BsonField("Name")] private string? name;

    [MemberNotNullWhen(false, nameof(name))]
    [JsonIgnore]
    [BsonIgnore]
    public bool NameIsUnknown => name is null;

    [BsonIgnore]
    public string Name {
        get => name ?? "Unknown";
        set => name = value;
    }

    [BsonId] public string SystemAddress { get; init; }

    private IDictionary<string, Body> bodies = new ConcurrentDictionary<string, Body>();

    [JsonIgnore] [BsonIgnore] private readonly object bodiesLock = new();


    public IDictionary<string, Body> Bodies {
        get => bodies;
        set => bodies = value.GetType() == typeof(ConcurrentDictionary<string, Body>)
            ? value
            : new ConcurrentDictionary<string, Body>(value);
    }

    [JsonIgnore] [BsonIgnore] public IReadOnlyDictionary<string, Body> BodyView => Bodies.AsReadOnly();


    [JsonIgnore]
    [BsonIgnore]
    public ImmutableDictionary<string, BodyData.ValuePrediction> Predictions =>
        Bodies
            .Where(x => x.Value.Prediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.Prediction!);

    [JsonIgnore]
    [BsonIgnore]
    public ImmutableDictionary<string, BodyData.ValuePrediction> RefinedPredictions =>
        Bodies
            .Where(x => x.Value.RefinedPrediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.RefinedPrediction!);

    public IDictionary<string, Body> PredictionReadyBodies() {
        var result = new Dictionary<string, Body>();
        lock (bodiesLock) {
            foreach (var (name, body) in Bodies) {
                if (body.Data.PredictionReady() && body.Prediction == null) {
                    result[name] = body;
                }
            }
        }

        return result;
    }

    public IDictionary<string, Body> RefinedPredictionReadyBodies() {
        var result = new Dictionary<string, Body>();
        lock (bodiesLock) {
            foreach (var (name, body) in Bodies) {
                if (body.Data.RefinedPredictionReady() && body.RefinedPrediction == null) {
                    result[name] = body;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns>true if already encountered body</returns>
    private bool EncounterBody(string name) {
        lock (bodiesLock) {
            if (Bodies.ContainsKey(name)) {
                return true;
            }

            var body = new Body(name);
            Bodies[name] = body;
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="body"></param>
    /// <returns>true if already encountered body</returns>
    public bool UpdateBody(string name, BodyData body) {
        lock (bodiesLock) {
            var o = EncounterBody(name);
            Bodies[name].Data = body;
            return o;
        }
    }

    public bool UpdateBody(string name, Body body) {
        lock (bodiesLock) {
            var o = EncounterBody(name);
            Bodies[name] = Bodies[name].Merge(body);
            return o;
        }
    }

    public bool RemoveBody(string name) {
        lock (bodiesLock) {
            return Bodies.Remove(name, out _);
        }
    }

    public bool UpdatePrediction(string name, BodyData.ValuePrediction prediction) {
        lock (bodiesLock) {
            var o = EncounterBody(name);
            Bodies[name].Prediction = prediction;
            return o;
        }
    }

    public bool UpdateRefinedPrediction(string name,
        BodyData.ValuePrediction refinedPrediction,
        CancellationToken cancellationToken = default) {
        lock (bodiesLock) {
            var o = EncounterBody(name);
            Bodies[name].RefinedPrediction = refinedPrediction;
            return o;
        }
    }

    public bool UpdateBodySignals(string name, int bioSignalCount) {
        lock (bodiesLock) {
            var o = EncounterBody(name);
            Bodies[name].Data.Count = bioSignalCount;
            return o;
        }
    }

    public override string ToString() {
        return $"System {{{Name}, {SystemAddress}}}";
    }
}

public class Body {
    public Body(string name) {
        Name = name;
    }

    public string Name { get; }

    private BodyData data = new();
    public BodyData.ValuePrediction? Prediction { get; set; }
    public BodyData.ValuePrediction? RefinedPrediction { get; set; }


    public BodyData Data {
        get => data;
        set => data = data.Merge(value);
    }

    public static Body Merge(Body a, Body b) {
        var result = new Body(a.Name) {
            Data = a.Data.Merge(b.Data),
            Prediction = a.Prediction ?? b.Prediction,
            RefinedPrediction = a.RefinedPrediction ?? b.RefinedPrediction,
        };
        return result;
    }

    public Body Merge(Body other) {
        return Merge(this, other);
    }
}
