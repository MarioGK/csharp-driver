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
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;

using Cassandra.DataStax.Graph;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cassandra.Serialization.Graph.GraphSON2
{
    internal class GraphSONNode : INode
    {
        private readonly IGraphTypeSerializer _graphSerializer;

        private readonly JsonNode _token;

        private readonly string _graphsonType;

        public bool DeserializeGraphNodes => _graphSerializer.DefaultDeserializeGraphNodes;

        public bool IsArray => _token is JsonArray;

        public bool IsObjectTree => !IsScalar && !IsArray;

        public bool IsScalar => _token is JsonValue
                                || (_token is JsonObject jobj && jobj[GraphTypeSerializer.ValueKey] is JsonValue);

        public long Bulk { get; }

        internal GraphSONNode(IGraphTypeSerializer graphTypeSerializer, string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            _graphSerializer = graphTypeSerializer ?? throw new ArgumentNullException(nameof(graphTypeSerializer));
            var parsedJson = JsonNode.Parse(json)?.AsObject();
            _token = parsedJson["result"];
            if (_token is JsonObject jobj)
            {
                _graphsonType = jobj[GraphTypeSerializer.TypeKey]?.ToString();
            }

            var bulkToken = parsedJson["bulk"];
            Bulk = bulkToken != null ? GetTokenValue<long>(bulkToken) : 1L;
        }

        internal GraphSONNode(IGraphTypeSerializer graphTypeSerializer, JsonNode parsedGraphItem)
        {
            _graphSerializer = graphTypeSerializer ?? throw new ArgumentNullException(nameof(graphTypeSerializer));
            _token = parsedGraphItem ?? throw new ArgumentNullException(nameof(parsedGraphItem));
            if (_token is JsonObject jobj)
            {
                _graphsonType = jobj[GraphTypeSerializer.TypeKey]?.ToString();
            }
        }

        public T Get<T>(string propertyName, bool throwIfNotFound)
        {
            var value = GetPropertyValue(propertyName, throwIfNotFound);
            if (value == null)
            {
                if (default(T) != null)
                {
                    throw new NullReferenceException(string.Format(
                        "Cannot convert null to {0} because it is a value type, try using Nullable<{0}>",
                        typeof(T).Name));
                }
                return (T)(object)null;
            }
            return GetTokenValue<T>(value);
        }

        private JsonObject GetTypedValue()
        {
            if (_token is JsonObject jobj && GetGraphSONType() != null)
            {
                var value = jobj[GraphTypeSerializer.ValueKey];
                if (value != null)
                {
                    // The token represents a GraphSON2/GraphSON3 object {"@type": "g:...", "@value": {}}
                    return value as JsonObject;
                }
            }

            return null;
        }

        private JsonNode GetPropertyFromObject(JsonObject graphObject, string name)
        {
            if (graphObject == null)
            {
                return null;
            }
            graphObject.TryGetPropertyValue(name, out var val);
            return val;
        }

        private JsonNode GetPropertyValue(string name, bool throwIfNotFound)
        {
            var graphObject = _token as JsonObject;
            var typedValue = GetTypedValue();
            if (typedValue != null)
            {
                graphObject = typedValue;
            }
            if (graphObject == null)
            {
                throw new KeyNotFoundException($"Cannot retrieve property '{name}' of instance: '{_token}'");
            }
            if (!graphObject.TryGetPropertyValue(name, out var propertyValue))
            {
                if (throwIfNotFound)
                {
                    throw new KeyNotFoundException($"Graph result has no top-level property '{name}'");
                }
                return null;
            }
            return propertyValue;
        }

        public dynamic GetRaw()
        {
            return _token;
        }

        /// <summary>
        /// Returns either a scalar value or an array representing the token value, performing conversions when required.
        /// </summary>
        private T GetTokenValue<T>(JsonNode token)
        {
            return _graphSerializer.FromDb<T>(token);
        }

        private object GetTokenValue(JsonNode token, Type type)
        {
            return _graphSerializer.FromDb(token, type);
        }

        /// <summary>
        /// Returns true if the property is defined in this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the underlying value is not an object tree</exception>
        public bool HasProperty(string name)
        {
            var graphObject = _token as JsonObject;
            var typedValue = GetTypedValue();
            if (typedValue != null)
            {
                graphObject = typedValue;
            }
            return graphObject != null && graphObject.ContainsKey(name);
        }

        /// <summary>
        /// Provides the implementation for operations that get member values.
        /// </summary>
        public bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var token = GetPropertyValue(binder.Name, false);
            if (token == null)
            {
                result = null;
                return false;
            }
            var node = new GraphNode(new GraphSONNode(_graphSerializer, token));
            result = node.To(binder.ReturnType);
            return true;
        }

        /// <summary>
        /// Gets the hash code for this instance, based on its value.
        /// </summary>
        public override int GetHashCode()
        {
            return _token?.ToJsonString()?.GetHashCode() ?? 0;
        }

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException("Serializing GraphNodes in GraphSON2/GraphSON3 to JSON is not supported.");
        }

        /// <summary>
        /// Gets the a dictionary of properties of this node.
        /// </summary>
        public IDictionary<string, GraphNode> GetProperties()
        {
            return GetProperties<GraphNode>(_token);
        }

        /// <summary>
        /// Gets the a dictionary of properties of this node.
        /// </summary>
        public IDictionary<string, IGraphNode> GetIProperties()
        {
            return GetProperties<IGraphNode>(_token);
        }

        private IDictionary<string, T> GetProperties<T>(JsonNode item) where T : class, IGraphNode
        {
            var graphObject = GetTypedValue() ?? item as JsonObject;
            if (graphObject == null)
            {
                throw new InvalidOperationException($"Can not get properties from '{item}'");
            }
            return graphObject
                .ToDictionary(prop => prop.Key, prop => new GraphNode(new GraphSONNode(_graphSerializer, prop.Value)) as T);
        }

        /// <summary>
        /// Returns the representation of the <see cref="GraphNode"/> as an instance of the type provided.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Throws NotSupportedException when the target type is not supported
        /// </exception>
        public object To(Type type)
        {
            return GetTokenValue(_token, type);
        }

        /// <summary>
        /// Returns the representation of the <see cref="GraphNode"/> as an instance of the type provided.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Throws NotSupportedException when the target type is not supported
        /// </exception>
        public T To<T>()
        {
            return GetTokenValue<T>(_token);
        }

        /// <summary>
        /// Converts the instance into an array when the internal representation is a json array or a graphson list/set.
        /// </summary>
        public GraphNode[] ToArray()
        {
            return _graphSerializer.FromDb<GraphNode[]>(_token);
        }

        /// <summary>
        /// Returns the json representation of the result.
        /// </summary>
        public override string ToString()
        {
            if (_token is JsonValue val)
            {
                if (val.TryGetValue<string>(out var s))
                {
                    return s;
                }
                return val.ToString();
            }

            if (_token is JsonObject jobj)
            {
                var tokenValue = jobj[GraphTypeSerializer.ValueKey];

                switch (tokenValue)
                {
                    case null:
                        return string.Empty;

                    case JsonValue tokenValueVal:
                        if (tokenValueVal.TryGetValue<string>(out var sv))
                        {
                            return sv;
                        }
                        return tokenValueVal.ToString();

                    default:
                        return tokenValue.ToJsonString();
                }
            }

            return _token?.ToJsonString() ?? string.Empty;
        }

        public void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Serializing GraphNodes in GraphSON2/GraphSON3 to JSON is not supported.");
        }

        /// <inheritdoc />
        public string GetGraphSONType()
        {
            return _graphsonType;
        }
    }
}