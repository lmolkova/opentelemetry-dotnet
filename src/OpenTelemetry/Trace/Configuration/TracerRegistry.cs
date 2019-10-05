using System;
using System.Collections.Concurrent;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Trace.Configuration
{
    internal class TracerRegistry : TracerFactory
    {
        private static readonly ConcurrentDictionary<TracerRegistryKey, ITracer> Registry =
            new ConcurrentDictionary<TracerRegistryKey, ITracer>();

        private TracerBuilder defaultBuilder = new TracerBuilder(); // noop

        public TracerRegistry()
        {
            Tracing.GlobalInit += OnGlobalInit;
        }

        private void OnGlobalInit(object _, Tracing.GlobalInitEventArgs globalInitArgs)
        {
            this.defaultBuilder = globalInitArgs.GlobalTracerBuilder;
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
