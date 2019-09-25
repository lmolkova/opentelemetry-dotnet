// <copyright file="Tracer.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using OpenTelemetry.Context;
    using OpenTelemetry.Context.Propagation;
    using OpenTelemetry.Trace.Config;
    using OpenTelemetry.Trace.Export;
    using OpenTelemetry.Trace.Internal;

    /// <inheritdoc cref="ITracer"/>
    public sealed class Tracer : ITracer, IDisposable
    {
        private readonly MultiSpanProcessor multiSpanProcessor;

        static Tracer()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="ITracer"/>.
        /// </summary>
        /// <param name="spanProcessors">Span processors collection.</param>
        /// <param name="traceConfig">Trace configuration.</param>
        public Tracer(IEnumerable<SpanProcessor> spanProcessors, TraceConfig traceConfig)
        {
            // we accept only collection of processors to make it DI friendly. 
            // end-users in basic case are not expected to instantiate Tracer - it will be done by global default tracer
            if (spanProcessors == null)
            {
                throw new ArgumentNullException(nameof(spanProcessors));
            }

            this.multiSpanProcessor = new MultiSpanProcessor(spanProcessors);
            this.ActiveTraceConfig = traceConfig ?? throw new ArgumentNullException(nameof(traceConfig));
            this.BinaryFormat = new BinaryFormat();
            this.TextFormat = new TraceContextFormat();
        }

        /// <summary>
        /// Creates an instance of <see cref="Tracer"/>.
        /// </summary>
        /// <param name="spanProcessors">Span processor collection.</param>
        /// <param name="traceConfig">Trace configuration.</param>
        /// <param name="binaryFormat">Binary format context propagator.</param>
        /// <param name="textFormat">Text format context propagator.</param>
        public Tracer(IEnumerable<SpanProcessor> spanProcessors, TraceConfig traceConfig, IBinaryFormat binaryFormat, ITextFormat textFormat)
        {
            // we accept only collection of processors to make it DI friendly. 
            // end-users in basic case are not expected to instantiate Tracer - it will be done by global default tracer
            if (spanProcessors == null)
            {
                throw new ArgumentNullException(nameof(spanProcessors));
            }

            this.multiSpanProcessor = new MultiSpanProcessor(spanProcessors);
            this.ActiveTraceConfig = traceConfig ?? throw new ArgumentNullException(nameof(traceConfig));
            this.BinaryFormat = binaryFormat ?? throw new ArgumentNullException(nameof(binaryFormat));
            this.TextFormat = textFormat ?? throw new ArgumentNullException(nameof(textFormat));
        }

        /// <inheritdoc/>
        public ISpan CurrentSpan => CurrentSpanUtils.CurrentSpan;

        /// <inheritdoc/>
        public IBinaryFormat BinaryFormat { get; }

        /// <inheritdoc/>
        public ITextFormat TextFormat { get; }

        public TraceConfig ActiveTraceConfig { get; set; }

        /// <inheritdoc/>
        public ISpanBuilder SpanBuilder(string spanName)
        {
            return new SpanBuilder(spanName, this.multiSpanProcessor, this.ActiveTraceConfig);
        }

        public IScope WithSpan(ISpan span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            return CurrentSpanUtils.WithSpan(span, true);
        }

        public void Dispose()
        {
            this.multiSpanProcessor.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
