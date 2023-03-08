// EliteExplorationUtility - EEU.Monitor - AsyncLinesReader.cs
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

namespace EEU.Monitor.Util;

public class AsyncLinesReader : IAsyncEnumerable<string>, IDisposable {
    private readonly TextReader reader;

    public AsyncLinesReader(TextReader reader) {
        this.reader = reader;
    }

    public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = new()) {
        while (!cancellationToken.IsCancellationRequested) {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) {
                break;
            }

            yield return line;
        }
    }

    public void Dispose() {
        reader.Dispose();
        GC.SuppressFinalize(this);
    }
}
