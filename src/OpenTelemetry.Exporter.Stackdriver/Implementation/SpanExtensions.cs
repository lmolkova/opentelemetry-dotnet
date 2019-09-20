// <copyright file="SpanExtensions.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.Exporter.Stackdriver.Implementation
{
    using System.Linq;
    using Google.Cloud.Trace.V2;
    using Google.Protobuf.WellKnownTypes;
    using OpenTelemetry.Trace;

    internal static class SpanExtensions
    {
        /// <summary>
        /// Translating <see cref="Trace.Span"/> to Stackdriver's Span
        /// According to <see href="https://cloud.google.com/trace/docs/reference/v2/rpc/google.devtools.cloudtrace.v2"/> specifications.
        /// </summary>
        /// <param name="otSpan">Span in OpenTelemetry format.</param>
        /// <param name="projectId">Google Cloud Platform Project Id.</param>
        /// <returns><see cref="ISpan"/>.</returns>
        public static Google.Cloud.Trace.V2.Span ToSpan(this Trace.Span otSpan, string projectId)
        {
            var spanId = otSpan.Context.SpanId.ToHexString();

            // Base span settings
            var span = new Google.Cloud.Trace.V2.Span
            {
                SpanName = new SpanName(projectId, otSpan.Context.TraceId.ToHexString(), spanId),
                SpanId = spanId,
                DisplayName = new TruncatableString { Value = otSpan.Name },
                StartTime = otSpan.StartTimestamp.ToTimestamp(),
                EndTime = otSpan.EndTimestamp.ToTimestamp(),
                ChildSpanCount = null,
            };
            if (otSpan.ParentSpanId != default)
            {
                var parentSpanId = otSpan.ParentSpanId.ToHexString();
                if (!string.IsNullOrEmpty(parentSpanId))
                {
                    span.ParentSpanId = parentSpanId;
                }
            }

            // Span Links
            if (otSpan.Links != null)
            {
                span.Links = new Google.Cloud.Trace.V2.Span.Types.Links
                {
                    DroppedLinksCount = 0,
                    Link = { otSpan.Links.Select(l => l.ToLink()) },
                };
            }

            // Span Attributes
            if (otSpan.Attributes != null)
            {
                span.Attributes = new Google.Cloud.Trace.V2.Span.Types.Attributes
                {
                    DroppedAttributesCount = 0,

                    AttributeMap =
                    {
                        otSpan.Attributes?.ToDictionary(
                                        s => s.Key,
                                        s => s.Value?.ToAttributeValue()),
                    },
                };
            }

            return span;
        }

        public static Google.Cloud.Trace.V2.Span.Types.Link ToLink(this ILink link)
        {
            var ret = new Google.Cloud.Trace.V2.Span.Types.Link();
            ret.SpanId = link.Context.SpanId.ToHexString();
            ret.TraceId = link.Context.TraceId.ToHexString();

            if (link.Attributes != null)
            {
                ret.Attributes = new Google.Cloud.Trace.V2.Span.Types.Attributes
                {
                    DroppedAttributesCount = 0,

                    AttributeMap =
                    {
                        link.Attributes.ToDictionary(
                         att => att.Key,
                         att => att.Value.ToAttributeValue()),
                    },
                };
            }

            return ret;
        }

        public static Google.Cloud.Trace.V2.AttributeValue ToAttributeValue(this object av)
        {
            switch (av)
            {
                case string s:
                    return new Google.Cloud.Trace.V2.AttributeValue()
                    {
                        StringValue = new TruncatableString() { Value = s },
                    };
                case bool b:
                    return new Google.Cloud.Trace.V2.AttributeValue() { BoolValue = b };
                case long l:
                    return new Google.Cloud.Trace.V2.AttributeValue() { IntValue = l };
                case double d:
                    return new Google.Cloud.Trace.V2.AttributeValue()
                    {
                        StringValue = new TruncatableString() { Value = d.ToString() },
                    };
                default:
                    return new Google.Cloud.Trace.V2.AttributeValue()
                    {
                        StringValue = new TruncatableString() { Value = av.ToString() },
                    };
            }
        }
    }
}
