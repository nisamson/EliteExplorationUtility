using System.Text;
using System.Text.Json;
using FASTER.core;
using InvalidOperationException = System.InvalidOperationException;

namespace EEU.Monitor;

internal class SystemSerializer : BinaryObjectSerializer<System> {
    public override void Deserialize(out System obj) {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        var json = Encoding.UTF8.GetString(bytes);
        obj = JsonSerializer.Deserialize<System>(json) ?? throw new InvalidOperationException("Failed to deserialize System");
    }

    public override void Serialize(ref System obj) {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
