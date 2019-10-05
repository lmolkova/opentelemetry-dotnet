namespace OpenTelemetry.Trace.Configuration
{
    static class Tracing
    {
        internal static TracerRegistry registry;

        public static void Init(TracerBuilder builder)
        {
            registry = new TracerRegistry(builder);
        }
    }
}
