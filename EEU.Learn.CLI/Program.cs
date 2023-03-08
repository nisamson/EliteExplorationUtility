// EliteExplorationUtility - EEU.Learn.CLI - Program.cs
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
