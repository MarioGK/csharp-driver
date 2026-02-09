// 
//       Copyright (C) DataStax Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//       http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cassandra.Geometry
{
    internal class PolygonJsonConverter : JsonConverter<Polygon>
    {
        public override void Write(Utf8JsonWriter writer, Polygon value, JsonSerializerOptions options)
        {
            value.WriteJson(writer, options);
        }

        public override Polygon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new Polygon(doc.RootElement);
        }
    }
}