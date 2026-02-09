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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Cassandra.DataStax.Graph;

namespace Cassandra.Serialization.Graph.GraphSON1
{
    internal class GraphSON1Node : INode
    {
        private static readonly JsonSerializerOptions SerializerOptions = GraphSON1ContractResolver.Options;

        private readonly JsonNode _token;

        public bool DeserializeGraphNodes => true;

        public bool IsArray => _token is JsonArray;

        public bool IsObjectTree => _token is JsonObject;

        public bool IsScalar => _token is JsonValue;

        public long Bulk { get; }

        internal GraphSON1Node(string json, bool validateGraphson2)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            var parsedJson = JsonNode.Parse(json)?.AsObject();
            _token = parsedJson["result"];
            var bulkNode = parsedJson["bulk"];
            Bulk = bulkNode != null ? bulkNode.Deserialize<long>() : 1L;

            if (validateGraphson2)
            {
                if (_token is JsonObject jobj && jobj["@type"] != null)
                {
                    throw new NotSupportedException(
                        "Creating GraphNodes from raw json is not supported with GraphSON2/GraphSON3");
                }
            }
        }

        /// <summary>
        /// JsonNode and string have implicit conversions so making this ctor private makes it less error prone
        /// </summary>
        private GraphSON1Node(JsonNode parsedGraphItem)
        {
            _token = parsedGraphItem ?? throw new ArgumentNullException(nameof(parsedGraphItem));
        }

        internal static GraphSON1Node CreateParsedNode(JsonNode parsedGraphItem)
        {
            return new GraphSON1Node(parsedGraphItem);
        }

        internal static JsonNode ConvertToJsonNode(object value)
        {
            if (value == null) return null;
            if (value is JsonNode node) return node;
            if (value is IEnumerable<object> enumerable)
            {
                var arr = new JsonArray();
                foreach (var item in enumerable)
                {
                    arr.Add(ConvertToJsonNode(item));
                }
                return arr;
            }
            return JsonSerializer.SerializeToNode(value);
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

        private JsonNode GetPropertyValue(string name, bool throwIfNotFound)
        {
            if (!(_token is JsonObject))
            {
                if (_token is JsonValue jv)
                {
                    throw new KeyNotFoundException("Cannot retrieve properties of scalar value of type '{0}'" + jv.GetValueKind());
                }
                throw new KeyNotFoundException("Cannot retrieve properties of scalar value");
            }
            var graphObject = (JsonObject)_token;
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

        /// <summary>
        /// Gets the raw data represented by this instance.
        /// <para>
        /// Raw internal representation might be different depending on the graph serialization format and
        /// it is subject to change without any prior notice.
        /// </para>
        /// </summary>
        public dynamic GetRaw()
        {
            return _token;
        }

        /// <summary>
        /// Returns either a scalar value or an array representing the token value, performing conversions when required.
        /// </summary>
        private T GetTokenValue<T>(JsonNode token)
        {
            return (T)GetTokenValue(token, typeof(T));
        }

        private object GetTokenValue(JsonNode token, Type type)
        {
            try
            {
                if (type == typeof(object) || type == typeof(GraphNode))
                {
                    return new GraphNode(new GraphSON1Node(token));
                }
                if (token is JsonValue || token is JsonObject)
                {
                    if (type == typeof(TimeUuid))
                    {
                        // TimeUuid is not Serializable but convertible from Uuid
                        return (TimeUuid)token.Deserialize<Guid>(SerializerOptions);
                    }
                    return token.Deserialize(type, SerializerOptions);
                }
                if (token is JsonArray)
                {
                    Type elementType = null;
                    if (type.IsArray)
                    {
                        elementType = type.GetElementType();
                    }
                    else if (type.GetTypeInfo().IsGenericType &&
                             type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = type.GetTypeInfo().GetGenericArguments()[0];
                    }
                    return ToArray((JsonArray)token, elementType);
                }
            }
            catch (JsonException ex)
            {
                throw new NotSupportedException($"Type {type} is not supported", ex);
            }
            throw new NotSupportedException($"Token of type {token.GetType()} is not supported");
        }

        /// <summary>
        /// Returns either a JSON supported scalar value, a GraphNode or an Array of GraphNodes.
        /// </summary>
        private object GetTokenValue(JsonNode token)
        {
            if (token is JsonValue jv)
            {
                // Try to extract the underlying value
                if (jv.TryGetValue<string>(out var s)) return s;
                if (jv.TryGetValue<long>(out var l)) return l;
                if (jv.TryGetValue<double>(out var d)) return d;
                if (jv.TryGetValue<bool>(out var b)) return b;
                return jv.ToString();
            }
            if (token is JsonObject)
            {
                return new GraphNode(new GraphSON1Node(token));
            }
            if (token is JsonArray)
            {
                return ToArray((JsonArray)token);
            }
            throw new NotSupportedException($"Token of type {token.GetType()} is not supported");
        }

        /// <summary>
        /// Returns true if the property is defined in this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the underlying value is not an object tree</exception>
        public bool HasProperty(string name)
        {
            if (!(_token is JsonObject jobj))
            {
                return false;
            }
            return jobj.ContainsKey(name);
        }

        public string GetGraphSONType()
        {
            return null;
        }

        /// <summary>
        /// Provides the implementation for operations that get member values.
        /// </summary>
        public bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetTokenValue(GetPropertyValue(binder.Name, false));
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
            if (!IsObjectTree)
            {
                throw new NotSupportedException("Deserialization of GraphNodes that don't represent object trees is not supported");
            }
            foreach (var prop in (JsonObject)_token)
            {
                info.AddValue(prop.Key, prop.Value);
            }
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
            if (!(item is JsonObject jobj))
            {
                throw new InvalidOperationException($"Can not get properties from '{item}'");
            }
            return jobj
                .ToDictionary(prop => prop.Key, prop => new GraphNode(new GraphSON1Node(prop.Value)) as T);
        }

        /// <summary>
        /// Returns the representation of the <see cref="GraphNode"/> as an instance of the type provided.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Throws NotSupportedException when the target type is not supported
        /// </exception>
        public T To<T>()
        {
            return (T)GetTokenValue(_token, typeof(T));
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

        public Array ToArray(JsonArray jArray, Type elementType = null)
        {
            if (elementType == null)
            {
                elementType = typeof(GraphNode);
            }
            var arr = Array.CreateInstance(elementType, jArray.Count);
            var isGraphNode = elementType == typeof(GraphNode) || elementType == typeof(IGraphNode);
            for (var i = 0; i < arr.Length; i++)
            {
                var value = isGraphNode
                    ? new GraphNode(new GraphSON1Node(jArray[i]))
                    : jArray[i].Deserialize(elementType, SerializerOptions);
                arr.SetValue(value, i);
            }
            return arr;
        }

        /// <summary>
        /// Converts the instance into an array when the internal representation is a json array.
        /// </summary>
        public GraphNode[] ToArray()
        {
            if (!(_token is JsonArray))
            {
                throw new InvalidOperationException($"Cannot convert to array from {_token}");
            }
            return (GraphNode[])ToArray((JsonArray)_token);
        }

        /// <summary>
        /// Returns the json representation of the result.
        /// </summary>
        public override string ToString()
        {
            if (_token is JsonValue val)
            {
                return val.ToString();
            }

            return _token?.ToJsonString() ?? string.Empty;
        }

        public void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (!IsObjectTree)
            {
                throw new NotSupportedException(
                    "Deserialization of GraphNodes that don't represent object trees is not supported");
            }
            var json = _token.ToJsonString();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.WriteTo(writer);
        }
    }
}