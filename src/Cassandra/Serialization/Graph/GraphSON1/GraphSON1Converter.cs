//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cassandra.DataStax.Graph;
using Cassandra.Geometry;

namespace Cassandra.Serialization.Graph.GraphSON1
{
    internal class GraphSON1Converter : GraphSONConverter
    {
        private static readonly Dictionary<Type, WriteDelegate> Writers = new Dictionary<Type, WriteDelegate>
        {
            { typeof(GraphNode), WriteGraphNode },
            { typeof(IPAddress), WriteStringValue },
            { typeof(Point), WriteStringValue },
            { typeof(LineString), WriteStringValue },
            { typeof(Polygon), WriteStringValue },
            { typeof(BigInteger), WriteStringRawValue },
            { typeof(Duration), WriteDuration },
            { typeof(LocalDate), WriteStringValue },
            { typeof(LocalTime), WriteStringValue }
        };

        internal static readonly GraphSON1Converter Instance = new GraphSON1Converter();
        
        private readonly Dictionary<Type, ReadDelegate> _readers;

        private GraphSON1Converter()
        {
            _readers = new Dictionary<Type, ReadDelegate>
            {
                { typeof(IPAddress), (t) => IPAddress.Parse(t.ToString()) },
                { typeof(BigInteger), (t) => BigInteger.Parse(t.ToString(), CultureInfo.InvariantCulture) },
                { typeof(Point), (t) => Point.Parse(t.ToString()) },
                { typeof(LineString), (t) => LineString.Parse(t.ToString()) },
                { typeof(Polygon), (t) => Polygon.Parse(t.ToString()) },
                { typeof(Duration), (t) => Duration.Parse(t.ToString()) },
                { typeof(LocalDate), (t) => LocalDate.Parse(t.ToString()) },
                { typeof(LocalTime), (t) => LocalTime.Parse(t.ToString()) },
                { typeof(GraphNode), GetTokenReader(t => new GraphNode(GraphSON1Node.CreateParsedNode(t))) },
                { typeof(IGraphNode), GetTokenReader(t => new GraphNode(GraphSON1Node.CreateParsedNode(t))) },
                { typeof(Vertex), GetTokenReader(ToVertex) },
                { typeof(IVertex), GetTokenReader(ToVertex) },
                { typeof(Edge), GetTokenReader(ToEdge) },
                { typeof(IEdge), GetTokenReader(ToEdge) },
                { typeof(Path), GetTokenReader(ToPath) },
                { typeof(IPath), GetTokenReader(ToPath) },
                { typeof(IVertexProperty), GetTokenReader(ToVertexProperty) },
                { typeof(IProperty), GetTokenReader(ToProperty) }
            };
        }

        private ReadDelegate GetTokenReader<T>(Func<JsonNode, T> tokenReader)
        {
            object TokenReader(JsonNode token)
            {
                if (!(token is JsonObject))
                {
                    throw new InvalidOperationException($"Cannot create a {typeof(T).Name} from '{token}'");
                }
                return tokenReader(token);
            }
            return TokenReader;
        }

        public void WriteJson(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (!Writers.TryGetValue(value.GetType(), out WriteDelegate writeHandler))
            {
                return;
            }
            writeHandler(writer, value, options);
        }

        public object ReadJson(JsonNode token, Type objectType)
        {
            if (!_readers.TryGetValue(objectType, out ReadDelegate readHandler))
            {
                return null;
            }
            return readHandler(token);
        }

        protected override GraphNode ToGraphNode(JsonNode token)
        {
            return token == null ? null : new GraphNode(GraphSON1Node.CreateParsedNode(token));
        }

        public bool CanConvert(Type objectType)
        {
            return _readers.ContainsKey(objectType);
        }

        private static void WriteStringValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // GraphSON1 uses string representation for geometry types
            writer.WriteStringValue(value.ToString());
        }

        private static void WriteStringRawValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // ie: BigInteger
            writer.WriteRawValue(value.ToString());
        }

        private static void WriteGraphNode(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var graphNode = (GraphNode)value;
            if (!graphNode.IsObjectTree)
            {
                throw new NotSupportedException(
                    "Deserialization of GraphNodes that don't represent object trees is not supported");
            }
            JsonSerializer.Serialize(writer, graphNode.GetRaw(), options);
        }

        private static void WriteDuration(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var durationString = ((Duration)value).ToJavaDurationString();
            writer.WriteStringValue(durationString);
        }
    }
}
