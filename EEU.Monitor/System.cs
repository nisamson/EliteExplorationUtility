using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using EEU.Learn.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EEU.Monitor;

[DataContract]
public record System : IEquatable<System> {
    public System(string name, string systemAddress) {
        Name = name;
        SystemAddress = systemAddress;
        bodiesLock = new object();
    }

    public static System WithAddress(string systemAddress) {
        return new System("Unknown", systemAddress);
    }


    public System Merge(System other) {
        var result = new System(Name == "Unknown" ? other.Name : Name, SystemAddress);
        lock (BodiesLock) {
            foreach (var (name, body) in Bodies) {
                result.UpdateBody(name, body);
            }
        }

        lock (other.BodiesLock) {
            foreach (var (name, body) in other.Bodies) {
                result.UpdateBody(name, body);
            }
        }

        return result;
    }

    [DataMember] public string Name { get; set; }

    [DataMember] public string SystemAddress { get; init; }

    [JsonIgnore] [IgnoreDataMember] private object? bodiesLock;

    [DataMember] private IDictionary<string, Body> bodies = new ConcurrentDictionary<string, Body>();

    [JsonIgnore]
    [IgnoreDataMember]
    private object BodiesLock {
        get {
            if (bodiesLock == null) {
                lock (this) {
                    bodiesLock ??= new object();
                }
            }

            return bodiesLock;
        }
    }

    [IgnoreDataMember]
    public IDictionary<string, Body> Bodies {
        get => bodies;
        set => bodies = value.GetType() == typeof(ConcurrentDictionary<string, Body>)
            ? value
            : new ConcurrentDictionary<string, Body>(value);
    }

    [JsonIgnore] [IgnoreDataMember] public IReadOnlyDictionary<string, Body> BodyView => Bodies.AsReadOnly();


    [JsonIgnore]
    [IgnoreDataMember]
    public ImmutableDictionary<string, BodyData.ValuePrediction> Predictions =>
        Bodies
            .Where(x => x.Value.Prediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.Prediction!);

    [JsonIgnore]
    [IgnoreDataMember]
    public ImmutableDictionary<string, BodyData.ValuePrediction> RefinedPredictions =>
        Bodies
            .Where(x => x.Value.RefinedPrediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.RefinedPrediction!);

    public IDictionary<string, Body> PredictionReadyBodies() {
        var result = new Dictionary<string, Body>();
        lock (BodiesLock) {
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
        lock (BodiesLock) {
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
        lock (BodiesLock) {
            if (Bodies.ContainsKey(name)) {
                return true;
            }

            var body = new Body { Name = name };
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
        lock (BodiesLock) {
            var o = EncounterBody(name);
            Bodies[name].Data = body;
            return o;
        }
    }

    public bool UpdateBody(string name, Body body) {
        lock (BodiesLock) {
            var o = EncounterBody(name);
            Bodies[name] = Bodies[name].Merge(body);
            return o;
        }
    }

    public void RemoveBody(string name) {
        lock (BodiesLock) {
            Bodies.Remove(name, out _);
        }
    }

    public void UpdatePrediction(string name, BodyData.ValuePrediction prediction) {
        lock (BodiesLock) {
            EncounterBody(name);
            Bodies[name].Prediction = prediction;
        }
    }

    public void UpdateRefinedPrediction(string name, BodyData.ValuePrediction refinedPrediction) {
        lock (BodiesLock) {
            EncounterBody(name);
            Bodies[name].RefinedPrediction = refinedPrediction;
        }
    }

    public void UpdateBodySignals(string name, int bioSignalCount) {
        lock (BodiesLock) {
            EncounterBody(name);
            Bodies[name].Data.Count = bioSignalCount;
        }
    }

    public virtual bool Equals(System? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Equals(bodiesLock, other.bodiesLock) && bodies.Equals(other.bodies) && Name == other.Name &&
               SystemAddress == other.SystemAddress;
    }

    public override int GetHashCode() {
        lock (BodiesLock) {
            return HashCode.Combine(Bodies, Name, SystemAddress);
        }
    }
}

[DataContract]
public record Body([property: DataMember] string Name) {
    [DataMember] private BodyData data = new();
    [DataMember] private BodyData.ValuePrediction? prediction;
    [DataMember] private BodyData.ValuePrediction? refinedPrediction;

    [IgnoreDataMember]
    public BodyData Data {
        get => data;
        set => data = data.Merge(value);
    }

    [IgnoreDataMember]
    public BodyData.ValuePrediction? Prediction {
        get => prediction;
        set => prediction = value;
    }

    [IgnoreDataMember]
    public BodyData.ValuePrediction? RefinedPrediction {
        get => refinedPrediction;
        set => refinedPrediction = value;
    }

    public static Body Merge(Body a, Body b) {
        var result = new Body {
            Name = a.Name,
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
