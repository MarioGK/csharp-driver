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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cassandra.DataStax.Graph
{
    internal class GraphNodeConverter : JsonConverter<GraphNode>
    {
        public override void Write(Utf8JsonWriter writer, GraphNode value, JsonSerializerOptions options)
        {
            value.WriteJson(writer, options);
        }

        public override GraphNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonNode = JsonNode.Parse(ref reader);
            if (jsonNode is JsonObject jsonObject)
            {
                return new GraphNode(jsonObject);
            }
            // For non-object values (arrays, scalars), wrap in a result object so GraphSON1Node can parse
            var wrapper = new JsonObject { ["result"] = jsonNode };
            return new GraphNode(wrapper.ToJsonString());
        }
    }

    internal class GraphNodeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IGraphNode) || typeToConvert == typeof(GraphNode);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new GraphNodeConverter();
        }
    }
}