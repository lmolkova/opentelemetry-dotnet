﻿// <copyright file="Tracer.cs" company="OpenTelemetry Authors">
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
    using System.Diagnostics;

    using OpenTelemetry.Context.Propagation;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace.Configuration;
    using OpenTelemetry.Trace.Export;
    using OpenTelemetry.Trace.Internal;

    internal sealed class Tracer : ITracer
    {
        private readonly SpanProcessor spanProcessor;

        static Tracer()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="ITracer"/>.
        /// </summary>
        /// <param name="spanProcessor">Span processor.</param>
        /// <param name="tracerConfigurationOptions">Trace configuration.</param>
        public Tracer(SpanProcessor spanProcessor, TracerConfigurationOptions tracerConfigurationOptions) 
            : this(spanProcessor, tracerConfigurationOptions, new BinaryFormat(), new TraceContextFormat(), Resource.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="Tracer"/>.
        /// </summary>
        /// <param name="spanProcessor">Span processor.</param>
        /// <param name="tracerConfigurationOptions">Trace configuration.</param>
        /// <param name="binaryFormat">Binary format context propagator.</param>
        /// <param name="textFormat">Text format context propagator.</param>
        /// <param name="libraryResource">Resource describing the instrumentation library.</param>
        internal Tracer(SpanProcessor spanProcessor, TracerConfigurationOptions tracerConfigurationOptions, IBinaryFormat binaryFormat, ITextFormat textFormat, Resource libraryResource)
        {
            this.spanProcessor = spanProcessor ?? throw new ArgumentNullException(nameof(spanProcessor));
            this.ActiveTracerConfigurationOptions = tracerConfigurationOptions ?? throw new ArgumentNullException(nameof(tracerConfigurationOptions));
            this.BinaryFormat = binaryFormat ?? throw new ArgumentNullException(nameof(binaryFormat));
            this.TextFormat = textFormat ?? throw new ArgumentNullException(nameof(textFormat));
            this.LibraryResource = libraryResource ?? throw new ArgumentNullException(nameof(libraryResource));
        }

        public Resource LibraryResource { get; }

        /// <inheritdoc/>
        public ISpan CurrentSpan => CurrentSpanUtils.CurrentSpan;

        /// <inheritdoc/>
        public IBinaryFormat BinaryFormat { get; }

        /// <inheritdoc/>
        public ITextFormat TextFormat { get; }

        public TracerConfigurationOptions ActiveTracerConfigurationOptions { get; set; }

        /// <inheritdoc/>
        public ISpanBuilder SpanBuilder(string spanName)
        {
            return new SpanBuilder(spanName, this.spanProcessor, this.ActiveTracerConfigurationOptions, this.LibraryResource);
        }

        public IDisposable WithSpan(ISpan span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            return CurrentSpanUtils.WithSpan(span, true);
        }
    }
}
