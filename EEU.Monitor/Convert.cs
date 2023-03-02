using System.Diagnostics.CodeAnalysis;
using CaseExtensions;
using EEU.Learn.Model;
using EliteAPI.Events;
using Serilog;

namespace EEU.Monitor;

public static class Convert {
    public static double Or(this double value, double other, params double[] others) {
        if (value != 0) {
            return value;
        }

        return other != 0 ? other : others.FirstOrDefault(o => o != 0);
    }

    public static long Or(this long value, long other, params long[] others) {
        if (value != 0) {
            return value;
        }

        return other != 0 ? other : others.FirstOrDefault(o => o != 0);
    }

    [return: NotNullIfNotNull(nameof(value))]
    [return: NotNullIfNotNull(nameof(other))]
    public static string? Or(this string? value, string? other, params string?[] others) {
        if (!string.IsNullOrEmpty(value)) {
            return value;
        }

        return !string.IsNullOrEmpty(other) ? other : others.FirstOrDefault(o => !string.IsNullOrEmpty(o));
    }

    public static bool IsPlanetary(this ScanEvent scan) {
        return !string.IsNullOrEmpty(scan.PlanetClass);
    }

    public static bool IsDetailed(this ScanEvent scan) {
        return scan.ScanType == "Detailed";
    }

    public static bool IsDetailedPlanet(this ScanEvent scan) {
        return scan.IsPlanetary() && scan.IsDetailed();
    }

    public static BodyData? ToBodyData(this ScanEvent scan) {
        if (!scan.IsDetailedPlanet()) {
            return null;
        }

        var data = new BodyData {
            SubType = scan.PlanetClass,
            Gravity = scan.SurfaceGravity,
            SurfaceTemperature = scan.SurfaceTemperature,
            SurfacePressure = scan.SurfacePressure,
            Genera = "",
            DistanceToArrival = scan.DistanceFromArrivalLs,
            Ice = scan.Composition.Ice,
            Metal = scan.Composition.Metal,
            Rock = scan.Composition.Rock,
        };

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (scan.AtmosphereComposition != null) {
            foreach (var compPart in scan.AtmosphereComposition) {
                data.SetProperty(compPart.Name.ToPascalCase(), compPart.Percent, true);
            }
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (scan.Materials != null) {
            foreach (var compPart in scan.Materials) {
                data.SetProperty(compPart.Name.ToPascalCase(), compPart.Percent, false);
            }
        }

        return data;
    }

    public static bool PredictionReady(this BodyData body) {
        return body.Count != 0 && !string.IsNullOrEmpty(body.SubType);
    }

    public static BodyData Merge(this BodyData body, BodyData other) {
        var result = new BodyData();
        foreach (var prop in typeof(BodyData).GetProperties()) {
            var curVal = prop.GetValue(body);
            var candidateVal = prop.GetValue(other);
            switch (curVal) {
                case null when candidateVal != null:
                    prop.SetValue(result, candidateVal);
                    break;
                case double curD when candidateVal is double candD:
                    prop.SetValue(result, curD.Or(candD));
                    break;
                case long curL when candidateVal is long candL:
                    prop.SetValue(result, curL.Or(candL));
                    break;
                case string curS when candidateVal is string candS:
                    prop.SetValue(result, curS.Or(candS));
                    break;
            }
        }

        return result;
    }

    public static void SetProperty(this BodyData body, string name, double value, bool atmospheric = false) {
        Log.Logger.Verbose("Setting {Name} to {Value} on {Body}", name, value, body.SubType ?? "unknown");
        switch (name) {
            case "Iron":
                if (atmospheric) {
                    Log.Logger.Verbose("Atmospheric iron");
                    body.AtmosIron = value;
                } else {
                    Log.Logger.Verbose("Mats iron");
                    body.MatsIron = value;
                }

                break;

            default: {
                var prop = typeof(BodyData).GetProperty(name);
                if (prop != null) {
                    prop.SetValue(body, value);
                }

                break;
            }
        }
    }

    public static bool RefinedPredictionReady(this BodyData body) {
        return body.PredictionReady() && !string.IsNullOrEmpty(body.Genera);
    }
}
