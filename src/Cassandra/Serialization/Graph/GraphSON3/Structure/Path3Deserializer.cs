#region License

/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra.DataStax.Graph;
using Cassandra.DataStax.Graph.Internal;
using Cassandra.Serialization.Graph.GraphSON2;
using Cassandra.Serialization.Graph.GraphSON2.Structure;
using Cassandra.Serialization.Graph.Tinkerpop.Structure.IO.GraphSON;
using System.Text.Json.Nodes;

namespace Cassandra.Serialization.Graph.GraphSON3.Structure
{
    internal class Path3Deserializer : BaseStructureDeserializer, IGraphSONStructureDeserializer
    {
        private const string Prefix = "g";
        private const string TypeKey = "Path";
        
        public static string TypeName => 
            GraphSONUtil.FormatTypeName(Path3Deserializer.Prefix, Path3Deserializer.TypeKey);

        public dynamic Objectify(JsonNode graphsonObject, Func<JsonNode, GraphNode> factory, IGraphSONReader reader)
        {
            ICollection<ICollection<string>> labels = null;
            ICollection<GraphNode> objects = null;

            if (graphsonObject is JsonObject jObj)
            {
                labels = ParseLabels(jObj);
                objects = ParseObjects(jObj, factory);
            }
            
            return new Path(labels, objects);
        }

        private ICollection<ICollection<string>> ParseLabels(JsonObject tokenObj)
        {
            if (tokenObj["labels"] is JsonObject labelsObj 
                && labelsObj[GraphTypeSerializer.ValueKey] is JsonArray labelsArray)
            {
                return labelsArray
                       .Select(node =>
                       {
                          if (node is JsonObject nodeObj
                              && nodeObj[GraphTypeSerializer.ValueKey] is JsonArray nodeArray)
                          {
                              return new HashSet<string>(nodeArray.Select(n => n.ToString()));
                          }

                          throw new InvalidOperationException($"Cannot create a Path from {tokenObj}");
                      })
                      .ToArray();
            }

            return null;
        }

        private ICollection<GraphNode> ParseObjects(JsonObject tokenObj, Func<JsonNode, GraphNode> factory)
        {
            if (tokenObj["objects"] is JsonObject objectsObj 
                && objectsObj[GraphTypeSerializer.ValueKey] is JsonArray objectsArray)
            {
                return objectsArray.Select(jt => ToGraphNode(factory, jt)).ToArray();
            }

            return null;
        }
    }
}