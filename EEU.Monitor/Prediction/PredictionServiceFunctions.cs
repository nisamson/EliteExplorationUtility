// EliteExplorationUtility - EEU.Monitor - PredictionServiceFunctions.cs
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

using FASTER.core;

namespace EEU.Monitor.Prediction;

public class PredictionServiceFunctions : SimpleFunctions<string, Elite.System> {
    private PredictionServiceFunctions() : base(Merge) { }

    public static PredictionServiceFunctions Instance { get; } = new();

    private static Elite.System Merge(Elite.System a, Elite.System b) {
        return a.Merge(b);
    }
}
