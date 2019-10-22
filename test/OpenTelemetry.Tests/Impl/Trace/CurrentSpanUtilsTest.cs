// <copyright file="CurrentSpanUtilsTest.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Trace.Internal;
using System;
using System.Diagnostics;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Export;
using Xunit;

namespace OpenTelemetry.Trace.Test
{
    public class CurrentSpanUtilsTest: IDisposable
    {
        private readonly ITracer tracer;

        public CurrentSpanUtilsTest()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            tracer = TracerFactory.Create(b => { }).GetTracer(null);
        }

        [Fact]
        public void CurrentSpan_WhenNoContext()
        {
            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
        }

        [Fact]
        public void CurrentSpan_WhenNoSpanOnActivity()
        {
            var a = new Activity("foo").Start();
            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSpan_CloseDetaches(bool recordEvents)
        {
            var spanContext = new SpanContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), recordEvents ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);
            var span = (Span)tracer.StartSpan("foo", spanContext);

            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            using (this.tracer.WithSpan(span))
            {
                Assert.Same(span.Activity, Activity.Current);
                Assert.Same(span, this.tracer.CurrentSpan);
            }

            // span has not ended
            Assert.Equal(default, span.EndTimestamp);
            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            Assert.Null(Activity.Current);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSpan_NotOwningActivity(bool recordEvents)
        {
            var activity = new Activity("foo");
            if (recordEvents)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            activity.Start();
            var span = (Span)tracer.StartSpanFromActivity("foo", activity);

            Assert.Same(span.Activity, Activity.Current);
            Assert.Same(span, this.tracer.CurrentSpan);

            // TODO dispose
            // span has not ended
            Assert.Equal(default, span.EndTimestamp);

            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            Assert.Equal(activity, Activity.Current);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSpan_NoopOnBrokenScope(bool recordEvents)
        {
            var spanContext = new SpanContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), recordEvents ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);

            var parentSpan = (Span)tracer.StartSpan("parent", spanContext);
            var parentScope = this.tracer.WithSpan(parentSpan);

            var childSpan = (Span)tracer.StartSpan("child", parentSpan);
            var childActivity = childSpan.Activity;
            Assert.Same(parentSpan, this.tracer.CurrentSpan);

            var childScope = this.tracer.WithSpan(childSpan);

            parentScope.Dispose();

            Assert.Same(childSpan, this.tracer.CurrentSpan);
            Assert.Equal(childActivity, Activity.Current);

            // span has not ended
            Assert.Equal(default, childSpan.EndTimestamp);
            Assert.Equal(default, parentSpan.EndTimestamp);

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSpan_RestoresParentScope(bool recordEvents)
        {
            var spanContext = new SpanContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), recordEvents ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);

            var parentSpan = (Span)tracer.StartSpan("parent", spanContext);
            var parentActivity = parentSpan.Activity;
            var parentScope = this.tracer.WithSpan(parentSpan);

            var childSpan = (Span)tracer.StartSpan("child");
            Assert.Same(parentSpan, this.tracer.CurrentSpan);
            var childScope = this.tracer.WithSpan(childSpan);

            childScope.Dispose();

            Assert.Same(parentSpan, this.tracer.CurrentSpan);
            Assert.Equal(parentActivity, Activity.Current);

            // TODO
        }

        [Fact]
        public void WithSpan_SameActivityCreateScopeTwice()
        {
            var span = (Span)tracer.StartRootSpan("foo");

            using(this.tracer.WithSpan(span))
            using(this.tracer.WithSpan(span))
            {
                Assert.Same(span.Activity, Activity.Current);
                Assert.Same(span, this.tracer.CurrentSpan);
            }

            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            Assert.Null(Activity.Current);

            // TODO
        }

        [Fact]
        public void WithSpan_NullActivity()
        {
            var span = (Span)tracer.StartRootSpan("foo");

            span.Activity.Stop();

            using (this.tracer.WithSpan(span))
            {
                Assert.Null(Activity.Current);
                Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            }

            Assert.Null(Activity.Current);
            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);

            // TODO
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSpan_WrongActivity(bool recordEvents)
        {
            var spanContext = new SpanContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), recordEvents ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);

            var span = (Span)tracer.StartSpan("foo", spanContext);
            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            using (this.tracer.WithSpan(span))
            {
                Assert.Same(span.Activity, Activity.Current);
                Assert.Same(span, this.tracer.CurrentSpan);

                var anotherActivity = new Activity("foo").Start();
            }

            Assert.Same(BlankSpan.Instance, this.tracer.CurrentSpan);
            Assert.NotSame(span.Activity, Activity.Current);
            Assert.NotNull(Activity.Current);

            // TODO
        }

        public void Dispose()
        {
            Activity.Current = null;
        }
    }
}
