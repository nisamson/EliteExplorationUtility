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
