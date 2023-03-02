using System.Collections.Concurrent;
using System.Collections.Immutable;
using EEU.Learn.Model;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoSmart.AsyncLock;

namespace EEU.Monitor;

public record System {
    public System(string name, string systemAddress) {
        Name = name;
        SystemAddress = systemAddress;
    }

    public static System WithAddress(string systemAddress) {
        return new System("Unknown", systemAddress);
    }


    public async Task<System> MergeAsync(System other) {
        var result = new System(Name == "Unknown" ? other.Name : Name, SystemAddress);
        using (await bodiesLock.LockAsync()) {
            foreach (var (name, body) in Bodies) {
                await result.UpdateBody(name, body);
            }
        }

        using (await other.bodiesLock.LockAsync()) {
            foreach (var (name, body) in other.Bodies) {
                await result.UpdateBody(name, body);
            }
        }

        return result;
    }

    public System Merge(System other) {
        return MergeAsync(other).Result;
    }

    public string Name { get; set; }

    public string SystemAddress { get; init; }

    private IDictionary<string, Body> bodies = new ConcurrentDictionary<string, Body>();

    [JsonIgnore] private readonly AsyncLock bodiesLock = new();


    public IDictionary<string, Body> Bodies {
        get => bodies;
        set => bodies = value.GetType() == typeof(ConcurrentDictionary<string, Body>)
            ? value
            : new ConcurrentDictionary<string, Body>(value);
    }

    [JsonIgnore] public IReadOnlyDictionary<string, Body> BodyView => Bodies.AsReadOnly();


    [JsonIgnore]
    public ImmutableDictionary<string, BodyData.ValuePrediction> Predictions =>
        Bodies
            .Where(x => x.Value.Prediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.Prediction!);

    [JsonIgnore]
    public ImmutableDictionary<string, BodyData.ValuePrediction> RefinedPredictions =>
        Bodies
            .Where(x => x.Value.RefinedPrediction != null)
            .ToImmutableDictionary(x => x.Key, x => x.Value.RefinedPrediction!);

    public async Task<IDictionary<string, Body>> PredictionReadyBodies() {
        var result = new Dictionary<string, Body>();
        using (await bodiesLock.LockAsync()) {
            foreach (var (name, body) in Bodies) {
                if (body.Data.PredictionReady() && body.Prediction == null) {
                    result[name] = body;
                }
            }
        }

        return result;
    }

    public async Task<IDictionary<string, Body>> RefinedPredictionReadyBodies() {
        var result = new Dictionary<string, Body>();
        using (await bodiesLock.LockAsync()) {
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
    private async Task<bool> EncounterBody(string name) {
        using (await bodiesLock.LockAsync()) {
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
    public async Task<bool> UpdateBody(string name, BodyData body) {
        using (await bodiesLock.LockAsync()) {
            var o = await EncounterBody(name);
            Bodies[name].Data = body;
            return o;
        }
    }

    public async Task<bool> UpdateBody(string name, Body body) {
        using (await bodiesLock.LockAsync()) {
            var o = await EncounterBody(name);
            Bodies[name] = Bodies[name].Merge(body);
            return o;
        }
    }

    public async Task RemoveBody(string name) {
        using (await bodiesLock.LockAsync()) {
            Bodies.Remove(name, out _);
        }
    }

    public async Task UpdatePrediction(string name, BodyData.ValuePrediction prediction) {
        using (await bodiesLock.LockAsync()) {
            await EncounterBody(name);
            Bodies[name].Prediction = prediction;
        }
    }

    public async Task UpdateRefinedPrediction(string name, BodyData.ValuePrediction refinedPrediction) {
        using (await bodiesLock.LockAsync()) {
            await EncounterBody(name);
            Bodies[name].RefinedPrediction = refinedPrediction;
        }
    }

    public async Task UpdateBodySignals(string name, int bioSignalCount) {
        using (await bodiesLock.LockAsync()) {
            await EncounterBody(name);
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

        using (bodiesLock.Lock()) {
            return Bodies.Equals(other.Bodies) && Name == other.Name &&
                   SystemAddress == other.SystemAddress;
        }
    }

    public override int GetHashCode() {
        using (bodiesLock.Lock()) {
            return HashCode.Combine(Bodies, Name, SystemAddress);
        }
    }
}

public class Body {
    public string Name { get; init; } = "Unknown";
    private BodyData data = new();
    public BodyData.ValuePrediction? Prediction { get; set; }
    public BodyData.ValuePrediction? RefinedPrediction { get; set; }


    public BodyData Data {
        get => data;
        set => data = data.Merge(value);
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
