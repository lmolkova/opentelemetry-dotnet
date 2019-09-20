namespace OpenTelemetry.Exporter.Jaeger.Tests.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using OpenTelemetry.Exporter.Jaeger.Implementation;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Export;

    static class SpanHelper
    {
        internal static Span CreateTestSpan(bool setAttributes = true,
            bool addEvents = true,
            bool addLinks = true)
        {
            var startTimestamp = DateTime.UtcNow;
            var endTimestamp = startTimestamp.AddSeconds(60);
            var eventTimestamp = DateTime.UtcNow;
            var traceId = ActivityTraceId.CreateFromString("e8ea7e9ac72de94e91fabc613f9686b2".AsSpan());

            var parentSpanId = ActivitySpanId.CreateFromBytes(new byte[] { 12, 23, 34, 45, 56, 67, 78, 89 });
            var attributes = new Dictionary<string, object>
            {
                { "stringKey", "value"},
                { "longKey", 1L},
                { "longKey2", 1 },
                { "doubleKey", 1D},
                { "doubleKey2", 1F},
                { "boolKey", true},
            };
            var events = new List<IEvent>
            {
                Event.Create(
                    "Event1",
                    eventTimestamp,
                    new Dictionary<string, object>
                    {
                        { "key", "value" },
                    }
                ),
                Event.Create(
                    "Event2",
                    eventTimestamp,
                    new Dictionary<string, object>
                    {
                        { "key", "value" },
                    }
                )
            };

            var linkedSpanId = ActivitySpanId.CreateFromString("888915b6286b9c41".AsSpan());

            var link = Link.FromSpanContext(SpanContext.Create(
                    traceId,
                    linkedSpanId,
                    ActivityTraceFlags.Recorded,
                    Tracestate.Empty));

            var span = (Span)Tracing.Tracer
                .SpanBuilder("Name")
                .SetParent(SpanContext.Create(traceId, parentSpanId, ActivityTraceFlags.Recorded, Tracestate.Empty))
                .SetSpanKind(SpanKind.Client)
                .SetStartTimestamp(startTimestamp)
                .StartSpan();

            if (addLinks)
            {
                span.AddLink(link);
            }

            if (setAttributes)
            {
                foreach (var attribute in attributes)
                {
                    span.SetAttribute(attribute);
                }
            }

            if (addEvents)
            {
                foreach (var evnt in events)
                {
                    span.AddEvent(evnt);
                }
            }

            span.Status = Status.Ok;

            span.End(endTimestamp);
            return span;
        }
    }
}
