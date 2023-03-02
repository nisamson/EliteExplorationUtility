using System.Runtime.Serialization;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

namespace EEU.Learn.Model;

[Serializable]
[DataContract]
public record BodyData {
    private static readonly InputOutputColumnPair[] NumericColumns = {
        new(nameof(Gravity)),
        new(nameof(DistanceToArrival)),
        new(nameof(OrbitalEccentricity)),
        new(nameof(OrbitalInclination)),
        new(nameof(AxialTilt)),
        new(nameof(Helium)),
        new(nameof(Hydrogen)),
        new(nameof(CarbonDioxide)),
        new(nameof(Silicates)),
        new(nameof(SulphurDioxide)),
        new(nameof(Nitrogen)),
        new(nameof(Neon)),
        new(nameof(AtmosIron)),
        new(nameof(Argon)),
        new(nameof(Ammonia)),
        new(nameof(Methane)),
        new(nameof(Water)),
        new(nameof(Oxygen)),
        new(nameof(Antimony)),
        new(nameof(Arsenic)),
        new(nameof(Carbon)),
        new(nameof(MatsIron)),
        new(nameof(Nickel)),
        new(nameof(Niobium)),
        new(nameof(Phosphorus)),
        new(nameof(Sulphur)),
        new(nameof(Tin)),
        new(nameof(Zinc)),
        new(nameof(Zirconium)),
        new(nameof(Cadmium)),
        new(nameof(Manganese)),
        new(nameof(Mercury)),
        new(nameof(Tellurium)),
        new(nameof(Vanadium)),
        new(nameof(Chromium)),
        new(nameof(Germanium)),
        new(nameof(Molybdenum)),
        new(nameof(Ruthenium)),
        new(nameof(Yttrium)),
        new(nameof(Selenium)),
        new(nameof(Technetium)),
        new(nameof(Tungsten)),
        new(nameof(Polonium)),
        new(nameof(Rock)),
        new(nameof(Ice)),
        new(nameof(Metal)),
        new(nameof(Count)),
        new(nameof(SurfaceTemperature)),
        new(nameof(SurfacePressure)),
    };

    private static readonly InputOutputColumnPair[] InputColumns =
        NumericColumns.Append(new InputOutputColumnPair(nameof(SubType))).ToArray();

    private static readonly InputOutputColumnPair[] OutputColumns = {
        new(nameof(Value)),
    };

    private static readonly InputOutputColumnPair[] AllNumericIncludingValue = NumericColumns.Concat(OutputColumns).ToArray();

    public BodyData() : this("") { }

    public BodyData(string subType = "",
        double gravity = default,
        double distanceToArrival = default,
        double orbitalEccentricity = default,
        double orbitalInclination = default,
        double axialTilt = default,
        double helium = default,
        double hydrogen = default,
        double carbonDioxide = default,
        double silicates = default,
        double sulphurDioxide = default,
        double nitrogen = default,
        double neon = default,
        double atmosIron = default,
        double argon = default,
        double ammonia = default,
        double methane = default,
        double water = default,
        double oxygen = default,
        double antimony = default,
        double arsenic = default,
        double carbon = default,
        double matsIron = default,
        double nickel = default,
        double niobium = default,
        double phosphorus = default,
        double sulphur = default,
        double tin = default,
        double zinc = default,
        double zirconium = default,
        double cadmium = default,
        double manganese = default,
        double mercury = default,
        double tellurium = default,
        double vanadium = default,
        double chromium = default,
        double germanium = default,
        double molybdenum = default,
        double ruthenium = default,
        double yttrium = default,
        double selenium = default,
        double technetium = default,
        double tungsten = default,
        double polonium = default,
        double rock = default,
        double ice = default,
        double metal = default,
        long count = default,
        double surfaceTemperature = default,
        double surfacePressure = default,
        string genera = "",
        long value = default) {
        SubType = subType;
        Gravity = gravity;
        DistanceToArrival = distanceToArrival;
        OrbitalEccentricity = orbitalEccentricity;
        OrbitalInclination = orbitalInclination;
        AxialTilt = axialTilt;
        Helium = helium;
        Hydrogen = hydrogen;
        CarbonDioxide = carbonDioxide;
        Silicates = silicates;
        SulphurDioxide = sulphurDioxide;
        Nitrogen = nitrogen;
        Neon = neon;
        AtmosIron = atmosIron;
        Argon = argon;
        Ammonia = ammonia;
        Methane = methane;
        Water = water;
        Oxygen = oxygen;
        Antimony = antimony;
        Arsenic = arsenic;
        Carbon = carbon;
        MatsIron = matsIron;
        Nickel = nickel;
        Niobium = niobium;
        Phosphorus = phosphorus;
        Sulphur = sulphur;
        Tin = tin;
        Zinc = zinc;
        Zirconium = zirconium;
        Cadmium = cadmium;
        Manganese = manganese;
        Mercury = mercury;
        Tellurium = tellurium;
        Vanadium = vanadium;
        Chromium = chromium;
        Germanium = germanium;
        Molybdenum = molybdenum;
        Ruthenium = ruthenium;
        Yttrium = yttrium;
        Selenium = selenium;
        Technetium = technetium;
        Tungsten = tungsten;
        Polonium = polonium;
        Rock = rock;
        Ice = ice;
        Metal = metal;
        Count = count;
        SurfaceTemperature = surfaceTemperature;
        SurfacePressure = surfacePressure;
        Genera = genera;
        Value = value;
    }

    public static string[] FeatureColumnNames() {
        return NumericColumns.Select(x => x.OutputColumnName).ToArray();
    }

    public string SubType { get; set; }

    public double Gravity { get; set; }

    public double DistanceToArrival { get; set; }
    public double OrbitalEccentricity { get; set; }

    public double OrbitalInclination { get; set; }

    public double AxialTilt { get; set; }

    public double Helium { get; set; }

    public double Hydrogen { get; set; }

    public double CarbonDioxide { get; set; }

    public double Silicates { get; set; }

    public double SulphurDioxide { get; set; }

    public double Nitrogen { get; set; }

    public double Neon { get; set; }

    public double AtmosIron { get; set; }

    public double Argon { get; set; }

    public double Ammonia { get; set; }

    public double Methane { get; set; }

    public double Water { get; set; }

    public double Oxygen { get; set; }

    public double Antimony { get; set; }

    public double Arsenic { get; set; }

    public double Carbon { get; set; }

    public double MatsIron { get; set; }

    public double Nickel { get; set; }

    public double Niobium { get; set; }

    public double Phosphorus { get; set; }

    public double Sulphur { get; set; }

    public double Tin { get; set; }

    public double Zinc { get; set; }

    public double Zirconium { get; set; }

    public double Cadmium { get; set; }

    public double Manganese { get; set; }

    public double Mercury { get; set; }

    public double Tellurium { get; set; }

    public double Vanadium { get; set; }

    public double Chromium { get; set; }

    public double Germanium { get; set; }

    public double Molybdenum { get; set; }

    public double Ruthenium { get; set; }

    public double Yttrium { get; set; }

    public double Selenium { get; set; }

    public double Technetium { get; set; }

    public double Tungsten { get; set; }

    public double Polonium { get; set; }

    public double Rock { get; set; }

    public double Ice { get; set; }

    public double Metal { get; set; }

    public long Count { get; set; }

    public double SurfaceTemperature { get; set; }
    public double SurfacePressure { get; set; }

    public string Genera { get; set; }

    public long Value { get; set; }

    public static IEstimator<ITransformer> CreateUnknownGeneraEstimator(MLContext context) {
        var estimator = CreateBaseEstimator(context);

        return estimator
            .Append(context.Transforms.Concatenate("Features", InputColumns.Select(x => x.OutputColumnName).ToArray()))
            .Append(context.Regression.Trainers.FastTree(nameof(Value)));
    }

    private static IEstimator<ITransformer> CreateBaseEstimator(MLContext context) {
        return context.Transforms.Conversion.ConvertType(AllNumericIncludingValue)
            .Append(
                context.Transforms.Categorical.OneHotEncoding(
                    nameof(SubType),
                    outputKind: OneHotEncodingEstimator.OutputKind.Indicator,
                    keyOrdinality: ValueToKeyMappingEstimator.KeyOrdinality.ByValue
                )
            )
            .Append(context.Transforms.ReplaceMissingValues(NumericColumns))
            .Append(context.Transforms.NormalizeMinMax(NumericColumns))
            .AppendCacheCheckpoint(context);
    }

    public static IEstimator<ITransformer> CreateKnownGeneraEstimator(MLContext context) {
        var baseEstimator = CreateBaseEstimator(context);
        return baseEstimator
            .Append(
                context.Transforms.Text.FeaturizeText(
                    nameof(Genera),
                    new TextFeaturizingEstimator.Options {
                        CaseMode = TextNormalizingEstimator.CaseMode.Lower,
                        CharFeatureExtractor = null,
                        WordFeatureExtractor = new WordBagEstimator.Options {
                            NgramLength = 1,
                            UseAllLengths = false,
                            Weighting = NgramExtractingEstimator.WeightingCriteria.Tf,
                        },
                    }
                )
            )
            .Append(
                context.Transforms.Concatenate(
                    "Features",
                    InputColumns.Select(x => x.OutputColumnName).Append(nameof(Genera)).ToArray()
                )
            )
            .AppendCacheCheckpoint(context)
            .Append(context.Regression.Trainers.FastTree(nameof(Value)));
    }

    public static void TrainModel(MLContext context, IDataView view) {
        var rand = new Random();
        var shuffRand = new Random();
        foreach (var _ in Enumerable.Range(0, 1)) {
            var idx = rand.Next();
            var testTrainSplit = context.Data.TrainTestSplit(view, 0.01, seed: idx);
            var trainView = testTrainSplit.TrainSet;
            var testView = testTrainSplit.TestSet;
            var estimator = CreateUnknownGeneraEstimator(context);
            var withGeneraEstimator = CreateKnownGeneraEstimator(context);
            var model = estimator.Fit(trainView);
            var withGeneraModel = withGeneraEstimator.Fit(trainView);
            var predictions = model.Transform(testView);
            var withGeneraPredictions = withGeneraModel.Transform(testView);
            var metrics = context.Regression.Evaluate(predictions, nameof(Value));
            var withGeneraMetrics = context.Regression.Evaluate(withGeneraPredictions, nameof(Value));

            Console.WriteLine($"Seed Number: {idx}");
            Console.WriteLine("Without Genera");
            Console.WriteLine($"R^2: {metrics.RSquared}");
            Console.WriteLine($"RMS: {metrics.RootMeanSquaredError}");
            Console.WriteLine($"MAE: {metrics.MeanAbsoluteError}");
            Console.WriteLine($"MSE: {metrics.MeanSquaredError}");
            Console.WriteLine("With Genera");
            Console.WriteLine($"R^2: {withGeneraMetrics.RSquared}");
            Console.WriteLine($"RMS: {withGeneraMetrics.RootMeanSquaredError}");
            Console.WriteLine($"MAE: {withGeneraMetrics.MeanAbsoluteError}");
            Console.WriteLine($"MSE: {withGeneraMetrics.MeanSquaredError}");
            //
            // using (var outFile = File.CreateText($"predictions{idx}.csv")) {
            //     outFile.WriteLine("Features[0],Features[1],Score,Value");
            //     foreach (var prediction in context.Data.CreateEnumerable<TestPrediction>(predictions, false)) {
            //         outFile.WriteLine($"{prediction.Features[0]},{prediction.Features[1]},{prediction.Score},{prediction.Value}");
            //     }
            // }

            Console.WriteLine("Without Genera");
            var permutationFeatureImportance = context.Regression
                .PermutationFeatureImportance(
                    model,
                    predictions,
                    permutationCount: 3,
                    labelColumnName: "Value"
                );
            var featureImportance = permutationFeatureImportance.Select(
                    kv => {
                        var (s, x) = kv;
                        return new {
                            Name = s,
                            x.RSquared,
                            x.MeanAbsoluteError,
                            x.MeanSquaredError,
                            x.RootMeanSquaredError,
                        };
                    }
                ).OrderByDescending(x => Math.Abs(x.RSquared.Mean))
                .ToArray();
            var maxNameLength = featureImportance.Max(x => x.Name.Length);
            foreach (var x in featureImportance) {
                var name = x.Name.PadRight(maxNameLength);
                Console.WriteLine($"{name}: {x.RSquared.Mean}");
            }

            Console.WriteLine("With Genera");
            permutationFeatureImportance = context.Regression
                .PermutationFeatureImportance(
                    withGeneraModel,
                    withGeneraPredictions,
                    permutationCount: 3,
                    labelColumnName: "Value"
                );
            featureImportance = permutationFeatureImportance.Select(
                    kv => {
                        var (s, x) = kv;
                        return new {
                            Name = s,
                            x.RSquared,
                            x.MeanAbsoluteError,
                            x.MeanSquaredError,
                            x.RootMeanSquaredError,
                        };
                    }
                ).OrderByDescending(x => Math.Abs(x.RSquared.Mean))
                .ToArray();
            maxNameLength = featureImportance.Max(x => x.Name.Length);
            foreach (var x in featureImportance) {
                var name = x.Name.PadRight(maxNameLength);
                Console.WriteLine($"{name}: {x.RSquared.Mean}");
            }

            var path = Path.GetFullPath($"valuePredictionModel.zip");
            context.Model.Save(model, view.Schema, path);
            var path2 = Path.GetFullPath($"valuePredictionModelWithGenera.zip");
            context.Model.Save(withGeneraModel, view.Schema, path2);
            Console.WriteLine($"wrote model to {path}");
        }
    }

    private class TestPrediction {
        public float[] Features { get; set; }
        public float Value { get; set; }
        public float Score { get; set; }
    }

    public class ValuePrediction {
        public float Score { get; set; }
    }
}
