// EliteExplorationUtility - EEU.Monitor - SystemSerializer.cs
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

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FASTER.core;
using Microsoft.Extensions.Logging;

namespace EEU.Monitor.KvSupport;

public class SystemSerializer : BinaryObjectSerializer<Elite.System> {
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    private readonly ILogger? log;

    public SystemSerializer(ILogger? log = null) {
        this.log = log;
    }

    public override void Deserialize(out Elite.System obj) {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        var json = Encoding.UTF8.GetString(bytes);
        log?.LogTrace("Deserializing System: {json}", json);
        obj = JsonSerializer.Deserialize<Elite.System>(json, JsonSerializerOptions) ??
              throw new InvalidOperationException("Failed to deserialize System");
    }

    public override void Serialize(ref Elite.System obj) {
        var json = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        log?.LogTrace("Serializing System: {json}", json);
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
