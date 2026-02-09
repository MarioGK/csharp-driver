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
    internal class InsightsStartupData
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("applicationName")]
        public string ApplicationName { get; set; }

        [JsonPropertyName("applicationVersion")]
        public string ApplicationVersion { get; set; }

        [JsonPropertyName("contactPoints")]
        public Dictionary<string, List<string>> ContactPoints { get; set; }

        [JsonPropertyName("initialControlConnection")]
        public string InitialControlConnection { get; set; }

        [JsonPropertyName("protocolVersion")]
        public byte ProtocolVersion { get; set; }

        [JsonPropertyName("localAddress")]
        public string LocalAddress { get; set; }

        [JsonPropertyName("executionProfiles")]
        public Dictionary<string, ExecutionProfileInfo> ExecutionProfiles { get; set; }

        [JsonPropertyName("poolSizeByHostDistance")]
        public PoolSizeByHostDistance PoolSizeByHostDistance { get; set; }

        [JsonPropertyName("heartbeatInterval")]
        public long HeartbeatInterval { get; set; }

        [JsonPropertyName("compression")]
        [JsonConverter(typeof(CompressionTypeInsightsConverter))]
        public CompressionType Compression { get; set; }

        [JsonPropertyName("reconnectionPolicy")]
        public PolicyInfo ReconnectionPolicy { get; set; }

        [JsonPropertyName("ssl")]
        public SslInfo Ssl { get; set; }

        [JsonPropertyName("authProvider")]
        public AuthProviderInfo AuthProvider { get; set; }

        [JsonPropertyName("otherOptions")]
        public Dictionary<string, object> OtherOptions { get; set; }

        [JsonPropertyName("configAntiPatterns")]
        public Dictionary<string, string> ConfigAntiPatterns { get; set; }

        [JsonPropertyName("periodicStatusInterval")]
        public long PeriodicStatusInterval { get; set; }

        [JsonPropertyName("platformInfo")]
        public InsightsPlatformInfo PlatformInfo { get; set; }

        [JsonPropertyName("hostName")]
        public string HostName { get; set; }

        [JsonPropertyName("driverName")]
        public string DriverName { get; set; }

        [JsonPropertyName("applicationNameWasGenerated")]
        public bool ApplicationNameWasGenerated { get; set; }

        [JsonPropertyName("driverVersion")]
        public string DriverVersion { get; set; }

        [JsonPropertyName("dataCenters")]
        public HashSet<string> DataCenters { get; set; }
    }
}