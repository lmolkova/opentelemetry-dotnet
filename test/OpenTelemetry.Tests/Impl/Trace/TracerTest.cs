// <copyright file="TracerTest.cs" company="OpenTelemetry Authors">
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

using System.Threading;
using Moq;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.Trace.Test
{
    using System;
    using OpenTelemetry.Trace.Config;
    using OpenTelemetry.Trace.Export;
    using OpenTelemetry.Trace.Sampler;

    using Xunit;

    public class TracerTest
    {
        private const string SpanName = "MySpanName";
        private readonly SpanProcessor spanProcessor;
        private readonly TraceConfig traceConfig;
        private Tracer tracer;


        public TracerTest()
        {
            spanProcessor = new SimpleSpanProcessor(new NoopSpanExporter());
            traceConfig = TraceConfig.Default;
            tracer = new Tracer(new []{ spanProcessor }, traceConfig);
        }

        [Fact]
        public void CreateSpanBuilder()
        {
            var spanBuilder = tracer.SpanBuilder(SpanName);
            Assert.IsType<SpanBuilder>(spanBuilder);
        }

        [Fact]
        public void BadConstructorArgumentsThrow()
        {
            var noopProc = new SimpleSpanProcessor(new NoopSpanExporter());
            Assert.Throws<ArgumentNullException>(() => new Tracer(null, TraceConfig.Default));
            Assert.Throws<ArgumentNullException>(() => new Tracer(null, TraceConfig.Default, new BinaryFormat(), new TraceContextFormat()));

            Assert.Throws<ArgumentNullException>(() => new Tracer(new[] { noopProc }, null));
            Assert.Throws<ArgumentNullException>(() => new Tracer(new[] { noopProc }, null, new BinaryFormat(), new TraceContextFormat()));

            Assert.Throws<ArgumentNullException>(() => new Tracer(new[] { noopProc }, TraceConfig.Default, null, new TraceContextFormat()));
            Assert.Throws<ArgumentNullException>(() => new Tracer(new[] { noopProc }, TraceConfig.Default, new BinaryFormat(), null));

        }

        [Fact]
        public void CreateSpanBuilderWithNullName()
        {
            Assert.Throws<ArgumentNullException>(() => tracer.SpanBuilder(null));
        }

        [Fact]
        public void GetCurrentSpanBlank()
        {
            Assert.Same(BlankSpan.Instance, tracer.CurrentSpan);
        }

        [Fact]
        public void GetCurrentSpan()
        {
            var span = tracer.SpanBuilder("foo").StartSpan();
            using (tracer.WithSpan(span))
            {
                Assert.Same(span, tracer.CurrentSpan);
            }
            Assert.Same(BlankSpan.Instance, tracer.CurrentSpan);
        }

        [Fact]
        public void WithSpanNull()
        {
            Assert.Throws<ArgumentNullException>(() => tracer.WithSpan(null));
        }

        [Fact]
        public void GetTextFormat()
        {
            Assert.NotNull(tracer.TextFormat);
        }

        [Fact]
        public void GetBinaryFormat()
        {
            Assert.NotNull(tracer.BinaryFormat);
        }

        [Fact]
        public void GetActiveConfig()
        {
            var config = new TraceConfig(Samplers.NeverSample);
            tracer = new Tracer(new [] { spanProcessor }, config);
            Assert.Equal(config, tracer.ActiveTraceConfig);
        }

        [Fact]
        public void SetActiveConfig()
        {
            var config = new TraceConfig(Samplers.NeverSample);
            tracer.ActiveTraceConfig = config;
            Assert.Equal(config, tracer.ActiveTraceConfig);
        }

        [Fact]
        public void MultipleProcessors()
        {
            var processor1 = new Mock<SpanProcessor>(new NoopSpanExporter());
            var processor2 = new Mock<SpanProcessor>(new NoopSpanExporter());

            tracer = new Tracer(new[] { processor1.Object, processor2.Object }, TraceConfig.Default);
            var span = tracer.SpanBuilder("foo").StartSpan();
            span.End();

            processor1.Verify((p) => p.OnStart(It.IsAny<Span>()), Times.Once);
            processor1.Verify((p) => p.OnEnd(It.IsAny<Span>()), Times.Once);

            processor2.Verify((p) => p.OnStart(It.IsAny<Span>()), Times.Once);
            processor2.Verify((p) => p.OnEnd(It.IsAny<Span>()), Times.Once);
        }

        [Fact]
        public void DisposeShutsDownProcessors()
        {
            var processor1 = new Mock<SpanProcessor>(new NoopSpanExporter());
            var processor2 = new Mock<SpanProcessor>(new NoopSpanExporter());

            tracer = new Tracer(new[] { processor1.Object, processor2.Object }, TraceConfig.Default);
            var span = tracer.SpanBuilder("foo").StartSpan();
            span.End();

            tracer.Dispose();

            processor1.Verify((p) => p.ShutdownAsync(It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Once);
            processor2.Verify((p) => p.ShutdownAsync(It.Is<CancellationToken>(ct => ct == CancellationToken.None)), Times.Once);
        }

        // TODO test for sampler
    }
}
