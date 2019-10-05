// <copyright file="TracerBuilder.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace.Configuration
{
    using System;
    using System.Collections.Generic;
    using OpenTelemetry.Context.Propagation;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace.Export;
    using OpenTelemetry.Trace.Sampler;

    public class TracerBuilder : IDisposable
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private TracerConfigurationOptions tracerConfigurationOptions;
        private ISampler sampler;
        private Func<SpanExporter, SpanProcessor> processorFactory;
        private SpanExporter spanExporter;
        private SpanProcessor spanProcessor;
        private IBinaryFormat binaryFormat;
        private ITextFormat textFormat;
        private Tracer tracer;
        private List<Collector> collectorFactories;

        public TracerBuilder AddSampler(ISampler sampler)
        {
            this.sampler = sampler;
            return this;
        }

        public TracerBuilder AddExporter(SpanExporter spanExporter)
        {
            this.spanExporter = spanExporter;
            return this;
        }

        public TracerBuilder AddProcessor(Func<SpanExporter, SpanProcessor> processorFactory)
        {
            this.processorFactory = processorFactory;
            return this;
        }

        public TracerBuilder ConfigureTracerOptions(TracerConfigurationOptions options)
        {
            this.tracerConfigurationOptions = options;
            return this;
        }

        public TracerBuilder AddTextFormat(ITextFormat textFormat)
        {
            this.textFormat = textFormat;
            return this;
        }

        public TracerBuilder AddCollector<TCollector>(
            Func<ITracer, TCollector> collectorFactory)
            where TCollector : class
        {
            if (this.collectorFactories == null)
            {
                this.collectorFactories = new List<Collector>();
            }

            this.collectorFactories.Add(new Collector(typeof(TCollector).Name, null, collectorFactory));

            return this;
        }

        public ITracer Build()
        {
            if (this.tracer == null)
            {
                if (this.sampler == null)
                {
                    // TODO separate sampler from options
                    this.sampler = Samplers.AlwaysSample;
                }

                if (this.tracerConfigurationOptions == null)
                {
                    this.tracerConfigurationOptions = new TracerConfigurationOptions(this.sampler);
                }

                if (this.spanExporter == null)
                {
                    // TODO log warning
                    this.spanExporter = new NoopSpanExporter();
                }

                this.spanProcessor = this.processorFactory != null
                    ? this.processorFactory(this.spanExporter)
                    : new BatchingSpanProcessor(this.spanExporter);

                if (this.spanProcessor is IDisposable disposableProcessor)
                {
                    this.disposables.Add(disposableProcessor);
                }

                this.binaryFormat = new BinaryFormat();
                this.textFormat = new TraceContextFormat();

                this.tracer = new Tracer(
                    this.spanProcessor,
                    this.tracerConfigurationOptions,
                    this.binaryFormat,
                    this.textFormat,
                    Resource.Empty);
            }

            if (this.collectorFactories != null)
            {
                foreach (var collector in this.collectorFactories)
                {
                    // there is only one TracerFactory in the process. Ever.
                    var collectorTracer = TracerFactory.Default.GetTracer(collector.Name, collector.Version);
                    var collectorInstance = collector.Factory.Invoke(collectorTracer);

                    if (collectorInstance is IDisposable disposableCollector)
                    {
                        this.disposables.Add(disposableCollector);
                    }
                }
            }

            return this.tracer;
        }

        public void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                disposable.Dispose();
            }
        }

        internal ITracer Build(string name, string version)
        {
            return new Tracer(
                this.spanProcessor,
                this.tracerConfigurationOptions,
                this.binaryFormat,
                this.textFormat,
                new Resource(CreateLibraryResourceLabels(name, version)));
        }

        private static IEnumerable<KeyValuePair<string, string>> CreateLibraryResourceLabels(string name, string version)
        {
            var labels = new Dictionary<string, string> { { "name", name } };
            if (!string.IsNullOrEmpty(version))
            {
                labels.Add("version", version);
            }

            return labels;
        }

        private struct Collector
        {
            public readonly string Name;
            public readonly string Version;
            public readonly Func<ITracer, object> Factory;

            internal Collector(string name, string version, Func<ITracer, object> factory)
            {
                this.Name = name;
                this.Version = version;
                this.Factory = factory;
            }
        }
    }
}
