using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using EEU.Learn.Model;
using Microsoft.ML;

namespace EEU.Learn;

public sealed class Service {
    public delegate void HandlePrediction(Prediction prediction);

    public sealed class Job {
        public Job(string bodyName, BodyData data, HandlePrediction handlePrediction) {
            BodyName = bodyName;
            Data = data;
            HandlePrediction = handlePrediction;
        }

        public string BodyName { get; }
        public BodyData Data { get; }
        public HandlePrediction HandlePrediction { get; }
    }

    public sealed class Prediction {
        public string BodyName { get; init; }
        public float ValuePrediction { get; init; }
    }

    private readonly MLContext mlContext;
    private DataViewSchema schema;
    private ITransformer model;
    private PredictionEngine<BodyData, BodyData.ValuePrediction> predictionEngine;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly BufferBlock<Job> jobs = new();
    private Task queueTask;

    private Service() {
        mlContext = new MLContext();
    }

    public static Service Create(Stream modelStream) {
        var service = new Service();
        Task.Run(() => service.LoadModelAsync(modelStream));
        return service;
    }

    private async Task LoadModelAsync(Stream modelStream) {
        model = await Task.Run(() => mlContext.Model.Load(modelStream, out schema), cancellationTokenSource.Token);
        predictionEngine = mlContext.Model.CreatePredictionEngine<BodyData, BodyData.ValuePrediction>(model, schema);
        queueTask = Task.Run(() => HandleJobs(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    private async Task HandleJobs(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var job = await jobs.ReceiveAsync(cancellationToken);

                var prediction = predictionEngine.Predict(job.Data);
                job.HandlePrediction(new Prediction { BodyName = job.BodyName, ValuePrediction = prediction.Score });
            } catch (OperationCanceledException) {
                break;
            }
        }
    }

    public void Cancel() {
        lock (this) {
            jobs.Complete();
            cancellationTokenSource.Cancel();
        }
    }

    public async Task WaitForOutstandingJobsAsync() {
        jobs.Complete();
        await queueTask;
    }

    public bool EnqueueJob(Job job) {
        return jobs.Post(job);
    }

    public Task<bool> EnqueueJobAsync(Job job) {
        return jobs.SendAsync(job);
    }

    public void Dispose() {
        Cancel();
        lock (this) {
            cancellationTokenSource.Dispose();
        }
    }
}
