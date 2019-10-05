namespace OpenTelemetry.Trace.Configuration
{
    static class Tracing
    {
        private static TracerFactory _defaultFactory;

        public static void StartTracing(TracerFactory factory)
        {
            if (_defaultFactory == null)
            {
                _defaultFactory = factory;
                factory.MakeDefaultBuilder();
            }
        }

        public static void Shutdown()
        {
            _defaultFactory.Dispose();
            _defaultFactory = null;
        }
    }
}
