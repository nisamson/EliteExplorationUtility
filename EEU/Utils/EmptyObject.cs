// EliteExplorationUtility - EEU - EmptyObject.cs
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

using Newtonsoft.Json;

namespace EEU.Utils;

[JsonObject]
[JsonConverter(typeof(Converter))]
public class EmptyObject {
    static EmptyObject() {
        Instance = new EmptyObject();
    }

    public static EmptyObject Instance { get; }

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
