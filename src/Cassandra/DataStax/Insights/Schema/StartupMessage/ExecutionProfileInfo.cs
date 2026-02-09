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

using System.Collections.Generic;
using Cassandra.DataStax.Insights.Schema.Converters;
using System.Text.Json.Serialization;

namespace Cassandra.DataStax.Insights.Schema.StartupMessage
{
    internal class ExecutionProfileInfo
    {
        [JsonPropertyName("readTimeout")]
        public int? ReadTimeout { get; set; }
        
        [JsonPropertyName("retry")]
        public PolicyInfo Retry { get; set; }

        [JsonPropertyName("loadBalancing")]
        public PolicyInfo LoadBalancing { get; set; }

        [JsonPropertyName("speculativeExecution")]
        public PolicyInfo SpeculativeExecution { get; set; }

        [JsonPropertyName("consistency")]
        [JsonConverter(typeof(ConsistencyInsightsConverter))]
        public ConsistencyLevel? Consistency { get; set; }

        [JsonPropertyName("serialConsistency")]
        [JsonConverter(typeof(ConsistencyInsightsConverter))]
        public ConsistencyLevel? SerialConsistency { get; set; }

        [JsonPropertyName("graphOptions")]
        public Dictionary<string, object> GraphOptions { get; set; }
    }
}