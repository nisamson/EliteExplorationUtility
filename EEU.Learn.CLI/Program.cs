// See https://aka.ms/new-console-template for more information

using EEU.Learn.Model;
using EEU.Model;
using Microsoft.Data.SqlClient;
using Microsoft.ML;
using Microsoft.ML.Data;

internal static class Program {
    private static void Main(string[] args) {
        var context = new MLContext();

        var dbSource = new DatabaseSource(SqlClientFactory.Instance, EEUContext.DefaultConnString, "SELECT * FROM OmniView", 0);
        var data = context.Data.CreateDatabaseLoader<BodyData>()
            .Load(dbSource);

        var loadedData = context.Data.CreateEnumerable<BodyData>(data, false).ToList();
        var inMemData = context.Data.LoadFromEnumerable(loadedData);

        BodyData.TrainModel(context, inMemData);
    }
}
