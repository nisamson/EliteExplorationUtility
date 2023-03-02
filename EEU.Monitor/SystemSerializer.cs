using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FASTER.core;
using Microsoft.Extensions.Logging;
using InvalidOperationException = System.InvalidOperationException;

namespace EEU.Monitor;

internal class SystemSerializer : BinaryObjectSerializer<System> {
    private readonly ILogger? log;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public SystemSerializer(ILogger? log = null) : base() {
        this.log = log;
    }

    public override void Deserialize(out System obj) {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        var json = Encoding.UTF8.GetString(bytes);
        log?.LogTrace("Deserializing System: {json}", json);
        obj = JsonSerializer.Deserialize<System>(json, JsonSerializerOptions) ??
              throw new InvalidOperationException("Failed to deserialize System");
    }

    public override void Serialize(ref System obj) {
        var json = JsonSerializer.Serialize(obj, JsonSerializerOptions);
        log?.LogTrace("Serializing System: {json}", json);
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
