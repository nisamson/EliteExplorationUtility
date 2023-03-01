using Newtonsoft.Json;

namespace EEU.Utils;

[JsonObject]
[JsonConverter(typeof(Converter))]
public class EmptyObject {
    public static EmptyObject Instance { get; }

    static EmptyObject() {
        Instance = new EmptyObject();
    }

    public class Converter : JsonConverter<EmptyObject> {
        public static Converter Instance { get; } = new();

        public override void WriteJson(JsonWriter writer, EmptyObject? value, JsonSerializer serializer) {
            if (value is null) {
                writer.WriteNull();
            } else {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }

        public override EmptyObject ReadJson(JsonReader reader,
            Type objectType,
            EmptyObject? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            uint stackCnt = 1;
            while (stackCnt > 0) {
                reader.Read();
                switch (reader.TokenType) {
                    case JsonToken.StartObject:
                        stackCnt++;
                        break;
                    case JsonToken.EndObject:
                        stackCnt--;
                        break;
                }
            }

            return existingValue ?? EmptyObject.Instance;
        }
    }
}
