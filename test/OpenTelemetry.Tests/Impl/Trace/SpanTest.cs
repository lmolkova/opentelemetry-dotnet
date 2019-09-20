// <copyright file="SpanTest.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Tests;

namespace OpenTelemetry.Trace.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Moq;
    using OpenTelemetry.Abstractions.Utils;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Config;
    using Xunit;

    public class SpanTest : IDisposable
    {
        private const string SpanName = "MySpanName";
        private const string EventDescription = "MyEvent";

        private readonly IDictionary<string, object> attributes = new Dictionary<String, object>();
        private readonly List<KeyValuePair<string, object>> expectedAttributes;
        private readonly IStartEndHandler startEndHandler = Mock.Of<IStartEndHandler>();

        public SpanTest()
        {
            attributes.Add("MyStringAttributeKey", "MyStringAttributeValue");
            attributes.Add("MyLongAttributeKey", 123L);
            attributes.Add("MyBooleanAttributeKey", false);
            expectedAttributes = new List<KeyValuePair<string, object>>(attributes)
                {
                    new KeyValuePair<string, object>("MySingleStringAttributeKey", "MySingleStringAttributeValue"),
                };
        }

        [Fact]
        public void GetSpanContextFromActivity()
        {
            var tracestate = Tracestate.Builder.Set("k1", "v1").Build();
            var activity = new Activity(SpanName).Start();
            activity.TraceStateString = tracestate.ToString();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var span =
                Span.StartSpan(
                    activity,
                    tracestate,
                    SpanKind.Internal,
                    startEndHandler,
                    default);
            Assert.NotNull(span.Context);
            Assert.Equal(activity.TraceId, span.Context.TraceId);
            Assert.Equal(activity.SpanId, span.Context.SpanId);
            Assert.Equal(activity.ParentSpanId, ((Span)span).ParentSpanId);
            Assert.Equal(activity.ActivityTraceFlags, span.Context.TraceOptions);
            Assert.Same(tracestate, span.Context.Tracestate);
        }

        [Fact]
        public void GetSpanContextFromActivityRecordedWithParent()
        {
            var tracestate = Tracestate.Builder.Set("k1", "v1").Build();
            var parent = new Activity(SpanName).Start();
            var activity = new Activity(SpanName).Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var span =
                Span.StartSpan(
                    activity,
                    tracestate,
                    SpanKind.Internal,
                    startEndHandler,
                    default);
            Assert.NotNull(span.Context);
            Assert.Equal(activity.TraceId, span.Context.TraceId);
            Assert.Equal(activity.SpanId, span.Context.SpanId);
            Assert.Equal(activity.ParentSpanId, ((Span)span).ParentSpanId);
            Assert.Equal(activity.ActivityTraceFlags, span.Context.TraceOptions);
            Assert.Same(tracestate, span.Context.Tracestate);
        }

        [Fact]
        public void NoEventsRecordedAfterEnd()
        {
            var link = SpanContext.Create(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.None, Tracestate.Empty);

            var activity = new Activity(SpanName).Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var spanStartTime = PreciseTimestamp.GetUtcNow();
            var span = (Span) Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    PreciseTimestamp.GetUtcNow());
            var spaEndTime = PreciseTimestamp.GetUtcNow();
            span.End();
            // Check that adding trace events after Span#End() does not throw any exception and are not
            // recorded.
            foreach (var attribute in attributes)
            {
                span.SetAttribute(attribute);
            }

            span.SetAttribute(
                "MySingleStringAttributeKey",
                "MySingleStringAttributeValue");

            span.AddEvent(Event.Create(EventDescription));
            span.AddEvent(EventDescription, attributes);
            span.AddLink(Link.FromSpanContext(link));

            AssertApproxSameTimestamp(span.StartTimestamp, spanStartTime);
            Assert.Empty(span.Attributes);
            Assert.Empty(span.Events);
            Assert.Empty(span.Links);
            Assert.Equal(Status.Ok, span.Status);
            AssertApproxSameTimestamp(spaEndTime, span.EndTimestamp);
        }

        [Fact]
        public async Task ToSpanData_ActiveSpan()
        {
            var contextLink = SpanContext.Create(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.None, Tracestate.Empty);

            var activity = new Activity(SpanName)
                .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom())
                .Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var spanStartTime = PreciseTimestamp.GetUtcNow();
            var span = (Span) Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    spanStartTime);

            span.SetAttribute(
                "MySingleStringAttributeKey",
                "MySingleStringAttributeValue");
            foreach (var attribute in attributes)
            {
                span.SetAttribute(attribute);
            }

            var firstEventTime = PreciseTimestamp.GetUtcNow();
            span.AddEvent(Event.Create(EventDescription));
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            var secondEventTime = PreciseTimestamp.GetUtcNow();
            span.AddEvent(EventDescription, attributes);

            var link = Link.FromSpanContext(contextLink);
            span.AddLink(link);

            Assert.Equal(activity.TraceId, span.Context.TraceId);
            Assert.Equal(activity.SpanId, span.Context.SpanId);
            Assert.Equal(activity.ParentSpanId, span.ParentSpanId);
            Assert.Equal(activity.ActivityTraceFlags, span.Context.TraceOptions);
            Assert.Same(Tracestate.Empty, span.Context.Tracestate);

            Assert.Equal(SpanName, span.Name);
            Assert.Equal(activity.ParentSpanId, span.ParentSpanId);
            span.Attributes.AssertAreSame(expectedAttributes);
            Assert.Equal(2, span.Events.Count());

            AssertApproxSameTimestamp(span.Events.ToList()[0].Timestamp, firstEventTime);
            AssertApproxSameTimestamp(span.Events.ToList()[1].Timestamp, secondEventTime);

            Assert.Equal(Event.Create(EventDescription), span.Events.ToList()[0]);

            Assert.Equal(Event.Create(EventDescription, default, attributes), span.Events.ToList()[1]);
            Assert.Single(span.Links);
            Assert.Equal(link, span.Links.First());

            Assert.Equal(spanStartTime, span.StartTimestamp);

            Assert.True(span.Status.IsValid);
            Assert.Equal(default, span.EndTimestamp);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            startEndMock.Verify(s => s.OnStart(span), Times.Once);
        }

        [Fact]
        public async Task GoSpanData_EndedSpan()
        {
            var contextLink = SpanContext.Create(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.None, Tracestate.Empty);

            var activity = new Activity(SpanName)
                .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom())
                .Start();

            var spanStartTime = PreciseTimestamp.GetUtcNow();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            var span =
                (Span)Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    PreciseTimestamp.GetUtcNow());

            span.SetAttribute(
                "MySingleStringAttributeKey",
                "MySingleStringAttributeValue");
            foreach (var attribute in attributes)
            {
                span.SetAttribute(attribute);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
            var firstEventTime = PreciseTimestamp.GetUtcNow();
            span.AddEvent(Event.Create(EventDescription));

            await Task.Delay(TimeSpan.FromMilliseconds(100));
            var secondEventTime = PreciseTimestamp.GetUtcNow();
            span.AddEvent(EventDescription, attributes);

            var link = Link.FromSpanContext(contextLink);
            span.AddLink(link);
            span.Status = Status.Cancelled;

            var spanEndTime = PreciseTimestamp.GetUtcNow();
            span.End();

            Assert.Equal(activity.TraceId, span.Context.TraceId);
            Assert.Equal(activity.SpanId, span.Context.SpanId);
            Assert.Equal(activity.ParentSpanId, span.ParentSpanId);
            Assert.Equal(activity.ActivityTraceFlags, span.Context.TraceOptions);

            Assert.Equal(SpanName, span.Name);
            Assert.Equal(activity.ParentSpanId, span.ParentSpanId);

            span.Attributes.AssertAreSame(expectedAttributes);
            Assert.Equal(2, span.Events.Count());

            AssertApproxSameTimestamp(span.Events.ToList()[0].Timestamp, firstEventTime);
            AssertApproxSameTimestamp(span.Events.ToList()[1].Timestamp, secondEventTime);

            Assert.Equal(Event.Create(EventDescription), span.Events.ToList()[0]);

            Assert.Single(span.Links);
            Assert.Equal(link, span.Links.First());
            AssertApproxSameTimestamp(span.StartTimestamp, spanStartTime);
            Assert.Equal(Status.Cancelled, span.Status);
            AssertApproxSameTimestamp(spanEndTime, span.EndTimestamp);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            startEndMock.Verify(s => s.OnStart(span), Times.Once);
            startEndMock.Verify(s => s.OnEnd(span), Times.Once);
        }

        [Fact]
        public void Status_ViaSetStatus()
        {
            var activity = new Activity(SpanName).Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var span =
                (Span)Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default);

            Assert.Equal(Status.Ok, span.Status);
            ((Span)span).Status = Status.Cancelled;
            Assert.Equal(Status.Cancelled, span.Status);
            span.End();
            Assert.Equal(Status.Cancelled, span.Status);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            startEndMock.Verify(s => s.OnStart(span), Times.Once);
        }

        [Fact]
        public void status_ViaEndSpanOptions()
        {
            var activity = new Activity(SpanName).Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var span =
                (Span)Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default);

            Assert.Equal(Status.Ok, span.Status);
            ((Span)span).Status = Status.Cancelled;
            Assert.Equal(Status.Cancelled, span.Status);
            span.Status = Status.Aborted;
            span.End();
            Assert.Equal(Status.Aborted, span.Status);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            startEndMock.Verify(s => s.OnStart(span), Times.Once);
        }

        [Fact]
        public void BadArguments()
        {
            var activity = new Activity(SpanName).Start();
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

            var span =
                Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default);

            Assert.Throws<ArgumentException>(() => span.Status = new Status());
            Assert.Throws<ArgumentNullException>(() => span.UpdateName(null));
            Assert.Throws<ArgumentNullException>(() => span.SetAttribute(null, string.Empty));
            Assert.Throws<ArgumentNullException>(() => span.SetAttribute(string.Empty, null));
            Assert.Throws<ArgumentNullException>(() =>
                span.SetAttribute(null, "foo"));
            Assert.Throws<ArgumentNullException>(() => span.SetAttribute(null, 1L));
            Assert.Throws<ArgumentNullException>(() => span.SetAttribute(null, 0.1d));
            Assert.Throws<ArgumentNullException>(() => span.SetAttribute(null, true));
            Assert.Throws<ArgumentNullException>(() => span.AddEvent((string)null));
            Assert.Throws<ArgumentNullException>(() => span.AddEvent((IEvent)null));
            Assert.Throws<ArgumentNullException>(() => span.AddLink(null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EndSpanStopsActivity(bool recordEvents)
        {
            var parentActivity = new Activity(SpanName).Start();
            var activity = new Activity(SpanName).Start();
            if (recordEvents)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            var span =
                Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default,
                    ownsActivity: true);
            
            span.End();
            Assert.Same(parentActivity, Activity.Current);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EndSpanDoesNotStopActivityWhenDoesNotOwnIt(bool recordEvents)
        {
            var activity = new Activity(SpanName).Start();
            if (recordEvents)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            var span =
                Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default,
                    ownsActivity: false);

            span.End();
            Assert.Equal(recordEvents, span.HasEnded);
            Assert.Same(activity, Activity.Current);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EndSpanStopActivity_NotCurrentActivity(bool recordEvents, bool ownsActivity)
        {
            var activity = new Activity(SpanName).Start();
            if (recordEvents)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            var span =
                Span.StartSpan(
                    activity,
                    Tracestate.Empty,
                    SpanKind.Internal,
                    startEndHandler,
                    default,
                    ownsActivity: ownsActivity);

            var anotherActivity = new Activity(SpanName).Start();
            span.End();
            Assert.Equal(recordEvents, span.HasEnded);
            Assert.Same(anotherActivity, Activity.Current);
        }

        public void Dispose()
        {
            Activity.Current = null;
        }

        private void AssertApproxSameTimestamp(DateTime one, DateTime two)
        {
            var timeShift = Math.Abs((one - two).TotalMilliseconds);
            Assert.InRange(timeShift, 0, 20);
        }
    }
}
