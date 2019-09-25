﻿// <auto-generated/>

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using OpenTelemetry.Exporter.LightStep;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Config;
    using OpenTelemetry.Trace.Export;

    internal class TestLightstep
    {
        internal static object Run(string accessToken)
        {
            var exporter = new LightStepTraceExporter(
                new LightStepTraceExporterOptions
                {
                    AccessToken = accessToken,
                    ServiceName = "tracing-to-lightstep-service",
                });

            var tracer = new Tracer(new[] { new BatchingSpanProcessor(exporter) }, TraceConfig.Default);
            
            using (tracer.WithSpan(tracer.SpanBuilder("Main").StartSpan()))
            {
                tracer.CurrentSpan.SetAttribute("custom-attribute", 55);
                Console.WriteLine("About to do a busy work");
                for (int i = 0; i < 10; i++)
                {
                    DoWork(i, tracer);
                }
            }
            Thread.Sleep(10000);
            // 5. Gracefully shutdown the exporter so it'll flush queued traces to LightStep.
            exporter.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
            return null;
        }
        
        private static void DoWork(int i, Tracer tracer)
        {
            using (tracer.WithSpan(tracer.SpanBuilder("DoWork").StartSpan()))
            {
                // Simulate some work.
                var span = tracer.CurrentSpan;

                try
                {
                    Console.WriteLine("Doing busy work");
                    Thread.Sleep(1000);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // Set status upon error
                    span.Status = Status.Internal.WithDescription(e.ToString());
                }

                // Annotate our span to capture metadata about our operation
                var attributes = new Dictionary<string, object>();
                attributes.Add("use", "demo");
                span.AddEvent("Invoking DoWork", attributes);
            }
        }
    }
}
