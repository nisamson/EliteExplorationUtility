// EliteExplorationUtility - EEU - Iter.cs
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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace EEU.Utils;

public static class Iter {
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? t) {
        return t ?? Enumerable.Empty<T>();
    }

    public static void Let<T>(this T? elem, Action<T> action) {
        if (elem is not null) {
            action(elem);
        }
    }

    public static async Task LetAsync<T>(this T? elem, Func<T, Task> action) {
        if (elem is not null) {
            await action(elem);
        }
    }

    public static bool IsEmpty<T>(this IReadOnlyCollection<T> col) {
        return col.Count == 0;
    }

    public static bool IsNotEmpty<T>(this IReadOnlyCollection<T> col) {
        return !col.IsEmpty();
    }


    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? elems) {
        return elems == null || !elems.Any();
    }

    public static async IAsyncEnumerable<T[]> Chunks<T>(this IAsyncEnumerable<T> elems, int chunkSize) where T : class {
        var buf = new List<T>(chunkSize);
        await foreach (var elem in elems) {
            buf.Add(elem);
            if (buf.Count >= chunkSize) {
                yield return buf.ToArray();
                buf.Clear();
            }
        }

        if (buf.IsNotEmpty()) {
            yield return buf.ToArray();
        }
    }

    public static BufferBlock<T> ToBuffer<T>(this IEnumerable<T> elems, int maxBufSize) {
        var buf = new BufferBlock<T>(
            new DataflowBlockOptions {
                BoundedCapacity = maxBufSize,
            }
        );

        Task.Run(() => EnqueueAll(elems, buf));

        return buf;
    }

    private static async Task EnqueueAll<T>(this IEnumerable<T> elems, BufferBlock<T> buf) {
        try {
            foreach (var elem in elems) {
                await buf.SendAsync(elem);
            }
        } finally {
            buf.Complete();
        }
    }

    public static SingletonList<T> ToSingleton<T>(this T elem) {
        return new SingletonList<T>(elem);
    }

    public static IEnumerable<string> Lines(this TextReader rd) {
        var nextLine = rd.ReadLine();
        while (nextLine is not null) {
            yield return nextLine;
            nextLine = rd.ReadLine();
        }
    }
}

public class SingletonList<T> : IList<T> {
    public SingletonList(T theThing) {
        TheThing = theThing;
    }

    private T TheThing { get; }

    public IEnumerator<T> GetEnumerator() {
        yield return TheThing;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void Add(T item) {
        throw new NotSupportedException();
    }

    public void Clear() {
        throw new NotSupportedException();
    }

    public bool Contains(T item) {
        if (ReferenceEquals(this, item)) {
            return true;
        }

        return Equals(item);
    }

    public void CopyTo(T[] array, int arrayIndex) {
        throw new NotImplementedException();
    }

    public bool Remove(T item) {
        throw new NotSupportedException();
    }

    public int Count => 1;
    public bool IsReadOnly => true;

    public int IndexOf(T item) {
        if (Contains(item)) {
            return 0;
        }

        return -1;
    }

    public void Insert(int index, T item) {
        throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
        throw new NotSupportedException();
    }

    public T this[int index] {
        get => index == 0 ? TheThing : throw new IndexOutOfRangeException();
        set => throw new NotSupportedException();
    }
}
