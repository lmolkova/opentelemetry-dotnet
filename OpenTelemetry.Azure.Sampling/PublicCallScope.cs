using System;
using System.Threading;

namespace OpenTelemetry.Azure.Sampling
{
    public sealed class PublicCall : IDisposable
    {
        private static AsyncLocal<bool> isPublic = new AsyncLocal<bool>();

        internal PublicCall()
        {
            isPublic.Value = true;
        }

        public static IDisposable BeginScope()
        {
            return new PublicCall();
        }

        public void Dispose()
        {
            isPublic.Value = false;
        }

        public static bool IsPublicCall() => isPublic.Value;
    }
}
