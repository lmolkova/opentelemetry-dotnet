using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenTelemetry.Trace.Configuration
{
    public class TracerRegistry : TracerFactory, IDisposable
    {
        private static readonly ConcurrentDictionary<TracerRegistryKey, ITracer> Registry =
            new ConcurrentDictionary<TracerRegistryKey, ITracer>();

        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly TracerBuilder defaultBuilder;

        // TODO it has to be singleton
        public TracerRegistry(TracerBuilder tracerBuilder)
        {
            this.defaultBuilder = tracerBuilder;
            base.Init(this);
        }

        public override ITracer GetTracer(string name, string version = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return this.defaultBuilder.Build();
            }

            var key = new TracerRegistryKey(name, version);
            return Registry.GetOrAdd(key, this.defaultBuilder.Build(name, version));
        }


        public void Dispose()
        {
            // TODO synchronization
            var tracers = Registry.Values;
            foreach (var tracer in tracers)
            {
                if (tracer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            foreach (var d in this.disposables)
            {
                d.Dispose();
            }
        }

        private struct TracerRegistryKey
        {
            private readonly string name;
            private readonly string version;

            internal TracerRegistryKey(string name, string version)
            {
                this.name = name;
                this.version = version;
            }
        }
    }
}
