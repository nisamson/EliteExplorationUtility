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
        return new BodyData {
            Ammonia = body.Ammonia.Or(other.Ammonia),
            Argon = body.Argon.Or(other.Argon),
            Antimony = body.Antimony.Or(body.Antimony),
            Arsenic = body.Arsenic.Or(other.Arsenic),
            AtmosIron = body.AtmosIron.Or(other.AtmosIron),
            AxialTilt = body.AxialTilt.Or(other.AxialTilt),
            Cadmium = body.Cadmium.Or(other.Cadmium),
            Carbon = body.Carbon.Or(other.Carbon),
            CarbonDioxide = body.CarbonDioxide.Or(other.CarbonDioxide),
            Chromium = body.Chromium.Or(other.Chromium),
            Count = body.Count.Or(other.Count),
            DistanceToArrival = body.DistanceToArrival.Or(other.DistanceToArrival),
            Gravity = body.Gravity.Or(other.Gravity),
            Hydrogen = body.Hydrogen.Or(other.Hydrogen),
            Ice = body.Ice.Or(other.Ice),
            Germanium = body.Germanium.Or(other.Germanium),
            Helium = body.Helium.Or(other.Helium),
            Metal = body.Metal.Or(other.Metal),
            Mercury = body.Mercury.Or(other.Mercury),
            Manganese = body.Manganese.Or(other.Manganese),
            Molybdenum = body.Molybdenum.Or(other.Molybdenum),
            Nitrogen = body.Nitrogen.Or(other.Nitrogen),
            Nickel = body.Nickel.Or(other.Nickel),
            Oxygen = body.Oxygen.Or(other.Oxygen),
            Phosphorus = body.Phosphorus.Or(other.Phosphorus),
            Methane = body.Phosphorus.Or(other.Methane),
            Neon = body.Neon.Or(other.Neon),
            Niobium = body.Niobium.Or(other.Niobium),
            Rock = body.Rock.Or(other.Rock),
            Polonium = body.Polonium.Or(other.Polonium),
            Tellurium = body.Tellurium.Or(other.Tellurium),
            Vanadium = body.Vanadium.Or(other.Vanadium),
            Yttrium = body.Yttrium.Or(other.Yttrium),
            OrbitalEccentricity = body.OrbitalEccentricity.Or(other.OrbitalEccentricity),
            OrbitalInclination = body.OrbitalInclination.Or(other.OrbitalInclination),
            Genera = string.IsNullOrEmpty(body.Genera) ? other.Genera : body.Genera,
            SurfacePressure = body.SurfacePressure.Or(other.SurfacePressure),
            SurfaceTemperature = body.SurfaceTemperature.Or(other.SurfaceTemperature),
            SubType = string.IsNullOrEmpty(body.SubType) ? other.SubType : body.SubType,
            Ruthenium = body.Ruthenium.Or(other.Ruthenium),
            Selenium = body.Selenium.Or(other.Selenium),
            Silicates = body.Silicates.Or(other.Silicates),
            Sulphur = body.Sulphur.Or(other.Sulphur),
            Technetium = body.Technetium.Or(other.Technetium),
            Tin = body.Tin.Or(other.Tin),
            Tungsten = body.Tungsten.Or(other.Tungsten),
            Value = body.Value.Or(other.Value),
            Water = body.Water.Or(other.Water),
            Zinc = body.Zinc.Or(other.Water),
            Zirconium = body.Zirconium.Or(other.Zirconium),
            MatsIron = body.MatsIron.Or(other.MatsIron),
            SulphurDioxide = body.SulphurDioxide.Or(other.SulphurDioxide),
        };
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
                    Log.Logger.Verbose("Success matching property");
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
