// <copyright file="JaegerSpanConverterTest.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Jaeger.Tests.Implementation
{
    using System.Linq;
    using OpenTelemetry.Exporter.Jaeger.Implementation;
    using Xunit;

    public class JaegerSpanConverterTest
    {
        private const long MillisPerSecond = 1000L;
        private const long NanosPerMillisecond = 1000 * 1000;
        private const long NanosPerSecond = NanosPerMillisecond * MillisPerSecond;

        public JaegerSpanConverterTest()
        {
        }

        [Fact]
        public void JaegerSpanConverterTest_ConvertSpanToJaegerSpan_AllPropertiesSet()
        {
            var span = SpanHelper.CreateTestSpan();
            var traceIdAsInt = new Int128(span.Context.TraceId);
            var spanIdAsInt = new Int128(span.Context.SpanId);
            var linkTraceIdAsInt = new Int128(span.Links.Single().Context.TraceId);
            var linkSpanIdAsInt = new Int128(span.Links.Single().Context.SpanId);

            var jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("Name", jaegerSpan.OperationName);
            Assert.Equal(2, jaegerSpan.Logs.Count());

            Assert.Equal(traceIdAsInt.High, jaegerSpan.TraceIdHigh);
            Assert.Equal(traceIdAsInt.Low, jaegerSpan.TraceIdLow);
            Assert.Equal(spanIdAsInt.Low, jaegerSpan.SpanId);
            Assert.Equal(new Int128(span.ParentSpanId).Low, jaegerSpan.ParentSpanId);

            Assert.Equal(span.Links.Count(), jaegerSpan.References.Count());
            var references = jaegerSpan.References.ToArray();
            var jaegerRef = references[0];
            Assert.Equal(linkTraceIdAsInt.High, jaegerRef.TraceIdHigh);
            Assert.Equal(linkTraceIdAsInt.Low, jaegerRef.TraceIdLow);
            Assert.Equal(linkSpanIdAsInt.Low, jaegerRef.SpanId);

            Assert.Equal(0x1, jaegerSpan.Flags);

            Assert.Equal(span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.StartTime);
            Assert.Equal((span.EndTimestamp - span.StartTimestamp).TotalMilliseconds * 1000, jaegerSpan.Duration);

            var tags = jaegerSpan.JaegerTags.ToArray();
            var tag = tags[0];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal("stringKey", tag.Key);
            Assert.Equal("value", tag.VStr);
            tag = tags[1];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[2];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey2", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[3];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[4];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey2", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[5];
            Assert.Equal(JaegerTagType.BOOL, tag.VType);
            Assert.Equal("boolKey", tag.Key);
            Assert.Equal(true, tag.VBool);

            var logs = jaegerSpan.Logs.ToArray();
            var jaegerLog = logs[0];
            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);
            Assert.Equal(2, jaegerLog.Fields.Count());
            var eventFields = jaegerLog.Fields.ToArray();
            var eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event1", eventField.VStr);

            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);

            jaegerLog = logs[1];
            Assert.Equal(2, jaegerLog.Fields.Count());
            eventFields = jaegerLog.Fields.ToArray();
            eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event2", eventField.VStr);
        }

        [Fact]
        public void JaegerSpanConverterTest_ConvertSpanToJaegerSpan_NoAttributes()
        {
            var span = SpanHelper.CreateTestSpan(setAttributes: false);
            var traceIdAsInt = new Int128(span.Context.TraceId);
            var spanIdAsInt = new Int128(span.Context.SpanId);
            var linkTraceIdAsInt = new Int128(span.Links.Single().Context.TraceId);
            var linkSpanIdAsInt = new Int128(span.Links.Single().Context.SpanId);

            var jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("Name", jaegerSpan.OperationName);
            Assert.Equal(2, jaegerSpan.Logs.Count());

            Assert.Equal(traceIdAsInt.High, jaegerSpan.TraceIdHigh);
            Assert.Equal(traceIdAsInt.Low, jaegerSpan.TraceIdLow);
            Assert.Equal(spanIdAsInt.Low, jaegerSpan.SpanId);
            Assert.Equal(new Int128(span.ParentSpanId).Low, jaegerSpan.ParentSpanId);

            Assert.Equal(span.Links.Count(), jaegerSpan.References.Count());
            var references = jaegerSpan.References.ToArray();
            var jaegerRef = references[0];
            Assert.Equal(linkTraceIdAsInt.High, jaegerRef.TraceIdHigh);
            Assert.Equal(linkTraceIdAsInt.Low, jaegerRef.TraceIdLow);
            Assert.Equal(linkSpanIdAsInt.Low, jaegerRef.SpanId);

            Assert.Equal(0x1, jaegerSpan.Flags);

            Assert.Equal(span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.StartTime);
            Assert.Equal((span.EndTimestamp - span.StartTimestamp).TotalMilliseconds * 1000, jaegerSpan.Duration);

            Assert.Empty(jaegerSpan.JaegerTags);

            var logs = jaegerSpan.Logs.ToArray();
            var jaegerLog = logs[0];
            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);
            Assert.Equal(2, jaegerLog.Fields.Count());
            var eventFields = jaegerLog.Fields.ToArray();
            var eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event1", eventField.VStr);

            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);

            jaegerLog = logs[1];
            Assert.Equal(2, jaegerLog.Fields.Count());
            eventFields = jaegerLog.Fields.ToArray();
            eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event2", eventField.VStr);
        }

        [Fact]
        public void JaegerSpanConverterTest_ConvertSpanToJaegerSpan_NoEvents()
        {
            var span = SpanHelper.CreateTestSpan(addEvents: false);
            var traceIdAsInt = new Int128(span.Context.TraceId);
            var spanIdAsInt = new Int128(span.Context.SpanId);
            var linkTraceIdAsInt = new Int128(span.Links.Single().Context.TraceId);
            var linkSpanIdAsInt = new Int128(span.Links.Single().Context.SpanId);

            var jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("Name", jaegerSpan.OperationName);
            Assert.Empty(jaegerSpan.Logs);

            Assert.Equal(traceIdAsInt.High, jaegerSpan.TraceIdHigh);
            Assert.Equal(traceIdAsInt.Low, jaegerSpan.TraceIdLow);
            Assert.Equal(spanIdAsInt.Low, jaegerSpan.SpanId);
            Assert.Equal(new Int128(span.ParentSpanId).Low, jaegerSpan.ParentSpanId);

            Assert.Equal(span.Links.Count(), jaegerSpan.References.Count());
            var references = jaegerSpan.References.ToArray();
            var jaegerRef = references[0];
            Assert.Equal(linkTraceIdAsInt.High, jaegerRef.TraceIdHigh);
            Assert.Equal(linkTraceIdAsInt.Low, jaegerRef.TraceIdLow);
            Assert.Equal(linkSpanIdAsInt.Low, jaegerRef.SpanId);

            Assert.Equal(0x1, jaegerSpan.Flags);

            Assert.Equal(span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.StartTime);
            Assert.Equal(span.EndTimestamp.ToEpochMicroseconds()
                         - span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.Duration);

            var tags = jaegerSpan.JaegerTags.ToArray();
            var tag = tags[0];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal("stringKey", tag.Key);
            Assert.Equal("value", tag.VStr);
            tag = tags[1];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[2];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey2", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[3];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[4];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey2", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[5];
            Assert.Equal(JaegerTagType.BOOL, tag.VType);
            Assert.Equal("boolKey", tag.Key);
            Assert.Equal(true, tag.VBool);
        }

        [Fact]
        public void JaegerSpanConverterTest_ConvertSpanToJaegerSpan_NoLinks()
        {
            var span = SpanHelper.CreateTestSpan(addLinks: false);
            var traceIdAsInt = new Int128(span.Context.TraceId);
            var spanIdAsInt = new Int128(span.Context.SpanId);

            var jaegerSpan = span.ToJaegerSpan();

            Assert.Equal("Name", jaegerSpan.OperationName);
            Assert.Equal(2, jaegerSpan.Logs.Count());

            Assert.Equal(traceIdAsInt.High, jaegerSpan.TraceIdHigh);
            Assert.Equal(traceIdAsInt.Low, jaegerSpan.TraceIdLow);
            Assert.Equal(spanIdAsInt.Low, jaegerSpan.SpanId);
            Assert.Equal(new Int128(span.ParentSpanId).Low, jaegerSpan.ParentSpanId);

            Assert.Empty(jaegerSpan.References);

            Assert.Equal(0x1, jaegerSpan.Flags);

            Assert.Equal(span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.StartTime);
            Assert.Equal(span.EndTimestamp.ToEpochMicroseconds()
                         - span.StartTimestamp.ToEpochMicroseconds(), jaegerSpan.Duration);

            var tags = jaegerSpan.JaegerTags.ToArray();
            var tag = tags[0];
            Assert.Equal(JaegerTagType.STRING, tag.VType);
            Assert.Equal("stringKey", tag.Key);
            Assert.Equal("value", tag.VStr);
            tag = tags[1];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[2];
            Assert.Equal(JaegerTagType.LONG, tag.VType);
            Assert.Equal("longKey2", tag.Key);
            Assert.Equal(1, tag.VLong);
            tag = tags[3];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[4];
            Assert.Equal(JaegerTagType.DOUBLE, tag.VType);
            Assert.Equal("doubleKey2", tag.Key);
            Assert.Equal(1, tag.VDouble);
            tag = tags[5];
            Assert.Equal(JaegerTagType.BOOL, tag.VType);
            Assert.Equal("boolKey", tag.Key);
            Assert.Equal(true, tag.VBool);

            var logs = jaegerSpan.Logs.ToArray();
            var jaegerLog = logs[0];
            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);
            Assert.Equal(2, jaegerLog.Fields.Count());
            var eventFields = jaegerLog.Fields.ToArray();
            var eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event1", eventField.VStr);
            Assert.Equal(span.Events.First().Timestamp.ToEpochMicroseconds(), jaegerLog.Timestamp);

            jaegerLog = logs[1];
            Assert.Equal(2, jaegerLog.Fields.Count());
            eventFields = jaegerLog.Fields.ToArray();
            eventField = eventFields[0];
            Assert.Equal("key", eventField.Key);
            Assert.Equal("value", eventField.VStr);
            eventField = eventFields[1];
            Assert.Equal("description", eventField.Key);
            Assert.Equal("Event2", eventField.VStr);
        }
    }
}
