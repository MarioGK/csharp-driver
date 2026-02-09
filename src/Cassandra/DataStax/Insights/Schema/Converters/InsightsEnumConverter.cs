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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cassandra.DataStax.Insights.Schema.Converters
{
    /// <summary>
    /// This JsonConverter implementation serializes values to their string representation in Insights schema
    /// </summary>
    /// <typeparam name="TEnumType">Type of objects that this converter can convert.</typeparam>
    /// <typeparam name="TJsonType">Type of objects that will be created by this converter.</typeparam>
    internal abstract class InsightsEnumConverter<TEnumType, TJsonType> : JsonConverter<TEnumType>
    {
        private static readonly string TypeString = typeof(TEnumType).ToString();
        private static readonly Logger Logger = new Logger(typeof(InsightsEnumConverter<TEnumType, TJsonType>));

        protected abstract IReadOnlyDictionary<TEnumType, TJsonType> EnumToJsonValueMap { get; }

        /// <summary>
        /// Converts the provided value (of the specified input type) to the specified output type.
        /// </summary>
        /// <param name="value">Input value</param>
        /// <param name="output">Output variable where the output value will be stored.</param>
        /// <returns><code>true</code> if conversion was successful; <code>false</code> otherwise.</returns>
        public bool TryConvert(TEnumType value, out TJsonType output)
        {
            if (!EnumToJsonValueMap.TryGetValue(value, out output))
            {
                InsightsEnumConverter<TEnumType, TJsonType>.Logger.Error(
                    $"Unrecognized value for type { InsightsEnumConverter<TEnumType, TJsonType>.TypeString }.");
                return false;
            }

            return true;
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="System.Text.Json.Utf8JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, TEnumType value, JsonSerializerOptions options)
        {
            if (!TryConvert(value, out var enumValueJsonValue))
            {
                InsightsEnumConverter<TEnumType, TJsonType>.Logger.Error($"Unrecognized value for type { InsightsEnumConverter<TEnumType, TJsonType>.TypeString }.");
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, enumValueJsonValue, options);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="System.Text.Json.Utf8JsonReader" /> to read from.</param>
        /// <param name="typeToConvert">Type of the object.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The object value.</returns>
        public override TEnumType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var existingValueJson = JsonSerializer.Deserialize<TJsonType>(ref reader, options);

            foreach (var kvp in EnumToJsonValueMap)
            {
                if (kvp.Value.Equals(existingValueJson))
                {
                    return kvp.Key;
                }
            }

            throw new ArgumentException($"could not convert {existingValueJson} to {InsightsEnumConverter<TEnumType, TJsonType>.TypeString}");
        }
    }
}